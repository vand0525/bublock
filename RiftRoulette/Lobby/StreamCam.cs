using System.Numerics;
using Bublock.Modules.Restraint;
using Bublock.Modules.Spectate;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Rift;
using RiftRoulette.Round;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.Lobby;

public static class StreamCam
{
  public const int TickSeconds = 2;

  // Outside fly cam a park does not move the client; it is sent again this often in case the viewer presses C.
  public static readonly TimeSpan ReparkEvery = TimeSpan.FromSeconds(6);

  // The client needs a moment after MakeObserver before the camera moves it.
  public static readonly TimeSpan SeatGrace = TimeSpan.FromSeconds(3);

  // A hero pawn only seconds old may still be set up; following it preceded a client crash.
  public static readonly TimeSpan FollowGrace = TimeSpan.FromSeconds(5);

  public static readonly TimeSpan FollowRetry = TimeSpan.FromSeconds(10);

  // The park's teleport and angles land within AngleRepeatSeconds; the camera is judged after that.
  public static readonly TimeSpan ParkSettle = TimeSpan.FromSeconds(SpectateService.AngleRepeatSeconds + 0.25);

  public const float PlacedUnits = 100f;

  private static readonly Logger Log = BublockLog.For("Lobby");

  private static readonly Dictionary<ulong, CamState> States = [];

  private static readonly Dictionary<ulong, DateTime> SpawnedAt = [];

  private sealed class CamState
  {
    public bool Auto = true;
    public DateTime? SeatedAt;
    public ulong? LastFollowed;
    public ulong? PendingKiller;
    public DateTime? FollowSentAt;
    public bool FollowFailLogged;
    public RiftSide? ParkedSide;
    public DateTime? LastParkAt;
    public Vector3? ParkTarget;
    public bool ParkPending;
    public bool Placed;
    public bool Adjusting;
    public Vector3? LastPosition;
    public Vector3? LastAngles;
  }

  public static void Tick(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var now = DateTime.UtcNow;

    foreach (var admin in SeatedObservers())
    {
      var state = State(admin.PlayerSteamId);

      if (!state.Auto || (state.SeatedAt is { } seated && now - seated < SeatGrace))
        continue;

      Update(admin, state, timer, now, mode);
    }
  }

  public static void Seated(ulong steamId) => State(steamId).SeatedAt = DateTime.UtcNow;

  public static void NoteSpawn(ulong steamId) => SpawnedAt[steamId] = DateTime.UtcNow;

  public static void OnDeath(CCitadelPlayerController victim, CCitadelPlayerController? attacker, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var victimId = victim.PlayerSteamId;
    var killerId = attacker != null && attacker.PlayerSteamId != victimId ? attacker.PlayerSteamId : (ulong?)null;
    var watched = false;

    foreach (var admin in SeatedObservers())
    {
      var state = State(admin.PlayerSteamId);

      if (!state.Auto || state.LastFollowed != victimId)
        continue;

      state.PendingKiller = killerId;
      watched = true;
      Log.WithMode(mode).Debug(admin.ToPlayerRef(), "Stream camera target died Victim={Victim} Killer={Killer}", victim.PlayerName, attacker?.PlayerName ?? "none");
    }

    if (watched)
      timer.NextTick(() => Tick(timer, mode));
  }

  public static bool SetAuto(CCitadelPlayerController admin, bool on, ExecutionMode mode = ExecutionMode.Clean)
  {
    var state = State(admin.PlayerSteamId);
    var changed = state.Auto != on;

    States[admin.PlayerSteamId] = new CamState { Auto = on, SeatedAt = state.SeatedAt };
    Log.WithMode(mode).Info(admin.ToPlayerRef(), "Stream camera auto Auto={Auto}", on);
    return changed;
  }

  public static void ResetFraming(ExecutionMode mode = ExecutionMode.Clean)
  {
    StreamFramingStore.Reset(mode);

    foreach (var state in States.Values)
    {
      state.Placed = false;
      state.Adjusting = false;
    }

    Log.WithMode(mode).Info("Stream camera framing reset to the default");
  }

  public static void Forget(ulong steamId)
  {
    SpawnedAt.Remove(steamId);

    if (States.TryGetValue(steamId, out var state))
      States[steamId] = new CamState { Auto = state.Auto };
  }

  public static IReadOnlyList<string> Describe(CCitadelPlayerController admin)
  {
    var state = State(admin.PlayerSteamId);
    var target = SpectateService.Current(admin);
    var watching = Players.GetAll().FirstOrDefault(player => SpectateService.IsWatching(admin, player.GetHeroPawn()));
    var angles = SpectateService.Pose(admin)?.Angles;
    var saved = StreamFramingStore.All;

    return
    [
      $"Auto={state.Auto} | Seated={AdminSeat.IsSeated(admin.PlayerSteamId)} | Observer={SpectateService.IsObserving(admin)} | Mode={SpectateService.Mode(admin)} | FlyCam={SpectateService.IsFlyCam(admin)} | ViewAngle={(angles is { } angle ? $"{angle.X:0.#} {angle.Y:0.#}" : "unreadable")}",
      $"Watching={watching?.PlayerName ?? (target == null ? "none" : target.DesignerName)} | ParkedSide={(state.ParkedSide is { } side ? RiftSides.Name(side) : "-")} Placed={state.Placed} Adjusting={state.Adjusting} | WatchSide={RiftSides.Name(WatchSpot.Side)}",
      "Framing " + string.Join(" | ", new[] { RiftSide.Green, RiftSide.Yellow }.Select(side =>
        $"{RiftSides.Name(side)}={(saved.TryGetValue(side, out var pose) ? DescribePose(pose) : "default")}"))
    ];
  }

  private static void Update(CCitadelPlayerController admin, CamState state, ITimer timer, DateTime now, ExecutionMode mode)
  {
    if (SpectateService.Pose(admin) is not { } pose)
      return;

    var flyCam = SpectateService.IsFlyCam(admin);

    if (state.ParkPending)
    {
      if (state.LastParkAt is { } sent && now - sent < ParkSettle)
        return;

      state.ParkPending = false;
      state.Placed = flyCam && state.ParkTarget is { } target && Vector3.Distance(pose.Position, target) <= PlacedUnits;
      Remember(state, pose);
      Log.WithMode(mode).Debug(admin.ToPlayerRef(), "Stream camera park landed Placed={Placed} Position={Position}", state.Placed, pose.Position);
      return;
    }

    var moved = flyCam && HandMoved(state, pose);
    Remember(state, pose);

    var candidates = Candidates();

    if (candidates.Count > 0)
      FollowStep(admin, state, candidates, moved, now, mode);
    else
      ParkStep(admin, state, pose, flyCam, moved, timer, now, mode);
  }

  private static void FollowStep(CCitadelPlayerController admin, CamState state, List<CCitadelPlayerController> candidates, bool moved, DateTime now, ExecutionMode mode)
  {
    state.Placed = false;
    state.Adjusting = false;
    state.ParkedSide = null;

    // Never move the camera while the admin flies it: that crashed the client.
    if (moved)
      return;

    var ids = candidates.Select(player => player.PlayerSteamId).ToList();
    var currentId = CurrentId(admin, candidates);

    if (currentId == null && state.LastFollowed is { } followed && ids.Contains(followed) && state.FollowSentAt is { } sent)
    {
      if (!state.FollowFailLogged)
      {
        state.FollowFailLogged = true;
        Log.WithMode(mode).Info(admin.ToPlayerRef(), "Stream camera follow did not take Target={Target} Mode={Mode} FlyCam={FlyCam}", NameOf(followed), SpectateService.Mode(admin), SpectateService.IsFlyCam(admin));
      }

      if (now - sent < FollowRetry)
        return;
    }

    var choice = SpectateRule.Choose(currentId, state.PendingKiller, ids);

    if (choice.Reason == SpectateReason.Keep)
    {
      if (state.LastFollowed != choice.Target)
        Log.WithMode(mode).Info(admin.ToPlayerRef(), "Stream camera Reason={Reason} Target={Target}", "keep", NameOf(choice.Target));

      state.LastFollowed = choice.Target;
      state.PendingKiller = null;
      state.FollowFailLogged = false;
      return;
    }

    var target = candidates.First(player => player.PlayerSteamId == choice.Target);
    var retry = state.LastFollowed == target.PlayerSteamId;
    var accepted = SpectateService.Follow(admin, target, mode);

    state.LastFollowed = target.PlayerSteamId;
    state.PendingKiller = null;
    state.FollowSentAt = now;

    var reason = retry ? "retry" : choice.Reason == SpectateReason.Killer ? "killer" : "any";
    var log = Log.WithMode(mode);

    if (retry)
      log.Debug(admin.ToPlayerRef(), "Stream camera Reason={Reason} Target={Target} Accepted={Accepted} Mode={Mode}", reason, target.PlayerName, accepted, SpectateService.Mode(admin));
    else
      log.Info(admin.ToPlayerRef(), "Stream camera Reason={Reason} Target={Target} Accepted={Accepted} Mode={Mode}", reason, target.PlayerName, accepted, SpectateService.Mode(admin));

    if (!retry)
      state.FollowFailLogged = false;
  }

  private static void ParkStep(
    CCitadelPlayerController admin,
    CamState state,
    (Vector3 Position, Vector3? Angles) pose,
    bool flyCam,
    bool moved,
    ITimer timer,
    DateTime now,
    ExecutionMode mode)
  {
    state.LastFollowed = null;
    state.PendingKiller = null;

    var side = WatchSpot.Side;

    if (!flyCam)
    {
      state.Placed = false;
      state.Adjusting = false;

      if (state.ParkedSide == side && state.LastParkAt is { } last && now - last < ReparkEvery)
        return;

      Park(admin, state, side, state.ParkedSide == side ? "repark" : "park", pending: false, timer, now, mode);
      return;
    }

    switch (SpectateRule.FramingStep(state.Placed, state.Adjusting, moved, spotChanged: state.ParkedSide != side))
    {
      case FramingAction.Adjust:
        state.Adjusting = true;
        state.Placed = false;
        break;

      case FramingAction.Save:
        SaveFraming(admin, state, pose, state.ParkedSide ?? side, mode);
        break;

      case FramingAction.Park:
        Park(admin, state, side, "framing", pending: true, timer, now, mode);
        break;
    }
  }

  private static void Park(CCitadelPlayerController admin, CamState state, RiftSide side, string reason, bool pending, ITimer timer, DateTime now, ExecutionMode mode)
  {
    var (position, angle) = StreamFraming.ToWorld(WatchSpot.Location(side), StreamFramingStore.For(side));

    if (!SpectateService.Park(admin, position, angle, timer, mode))
      return;

    var firstForSide = state.ParkedSide != side;

    state.ParkedSide = side;
    state.LastParkAt = now;
    state.ParkTarget = position;
    state.ParkPending = pending;
    state.Placed = false;

    var log = Log.WithMode(mode);

    if (reason == "repark")
      log.Debug(admin.ToPlayerRef(), "Stream camera Reason={Reason} Side={Side} Mode={Mode}", reason, RiftSides.Name(side), SpectateService.Mode(admin));
    else if (firstForSide || pending)
      log.Info(admin.ToPlayerRef(), "Stream camera Reason={Reason} Side={Side} Mode={Mode}", reason, RiftSides.Name(side), SpectateService.Mode(admin));
  }

  private static void SaveFraming(CCitadelPlayerController admin, CamState state, (Vector3 Position, Vector3? Angles) pose, RiftSide side, ExecutionMode mode)
  {
    var anchor = WatchSpot.Location(side);
    var previous = StreamFramingStore.For(side);
    var angles = pose.Angles ?? new Vector3(previous.Pitch, SpectateRule.WrapDegrees(anchor.Angle.Y + previous.Yaw), 0f);
    var framing = StreamFraming.FromWorld(anchor, pose.Position, angles);

    StreamFramingStore.Save(side, framing, mode);

    state.Adjusting = false;
    state.Placed = true;
    state.ParkedSide = side;

    Log.WithMode(mode).Info(
      admin.ToPlayerRef(),
      "Stream camera framing saved Side={Side} Offset={Offset} Pitch={Pitch} Yaw={Yaw} AngleRead={AngleRead}",
      RiftSides.Name(side),
      framing.Offset,
      framing.Pitch,
      framing.Yaw,
      pose.Angles != null);
  }

  private static bool HandMoved(CamState state, (Vector3 Position, Vector3? Angles) pose)
  {
    var distance = state.LastPosition is { } last ? Vector3.Distance(pose.Position, last) : 0f;
    var turned = state.LastAngles is { } from && pose.Angles is { } to ? SpectateRule.Turned(from, to) : 0f;

    return SpectateRule.HandMoved(distance, turned);
  }

  private static void Remember(CamState state, (Vector3 Position, Vector3? Angles) pose)
  {
    state.LastPosition = pose.Position;
    state.LastAngles = pose.Angles ?? state.LastAngles;
  }

  private static string DescribePose(CameraPose pose) =>
    $"offset {pose.Offset.X:0} {pose.Offset.Y:0} {pose.Offset.Z:0}, pitch {pose.Pitch:0.#}, yaw {pose.Yaw:0.#}";

  // During a round, restrained players are waiting up top; the camera prefers players who can fight.
  private static List<CCitadelPlayerController> Candidates()
  {
    var now = DateTime.UtcNow;
    var live = Participants.Humans()
      .Where(player => player.GetHeroPawn() is { } pawn && pawn.IsAlive)
      .Where(player => SpectateRule.FollowReady(SpawnedAt.GetValueOrDefault(player.PlayerSteamId), now, FollowGrace))
      .OrderBy(_ => Random.Shared.Next())
      .ToList();

    var fighting = live.Where(player => !RestraintService.IsRestrained(player.PlayerSteamId)).ToList();
    return fighting.Count > 0 ? fighting : live;
  }

  private static ulong? CurrentId(CCitadelPlayerController admin, List<CCitadelPlayerController> candidates) =>
    candidates.FirstOrDefault(player => SpectateService.IsWatching(admin, player.GetHeroPawn()))?.PlayerSteamId;

  private static IEnumerable<CCitadelPlayerController> SeatedObservers() =>
    Players.GetAll().Where(player => AdminSeat.IsSeated(player.PlayerSteamId) && SpectateService.IsObserving(player));

  private static CamState State(ulong steamId)
  {
    if (!States.TryGetValue(steamId, out var state))
      States[steamId] = state = new CamState();

    return state;
  }

  private static string NameOf(ulong? steamId) =>
    steamId is { } id ? Players.GetAll().FirstOrDefault(player => player.PlayerSteamId == id)?.PlayerName ?? id.ToString() : "none";
}
