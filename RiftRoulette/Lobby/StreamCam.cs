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

  // Above the players' floor up top (z 1536), so the straight-down view covers the platform.
  public const float OverheadHeight = 264f;

  public static readonly TimeSpan ReparkEvery = TimeSpan.FromSeconds(6);

  // The client needs a moment after MakeObserver before it takes spectator commands.
  public static readonly TimeSpan SeatGrace = TimeSpan.FromSeconds(3);

  // Moving the camera by hand while the server also drives it has crashed the client.
  public static readonly TimeSpan ManualHold = TimeSpan.FromSeconds(60);

  // Client commands the camera sends itself can echo back as console commands.
  public static readonly TimeSpan OwnCommandEcho = TimeSpan.FromSeconds(2);

  // A hero pawn only seconds old may still be set up; following it preceded a client crash.
  public static readonly TimeSpan FollowGrace = TimeSpan.FromSeconds(5);

  private const string SpectatorCommandPrefix = "spec_";

  // Observer movement between 2 s ticks that counts as the viewer still flying.
  private const float ManualMoveUnits = 50f;

  private static readonly Logger Log = BublockLog.For("Lobby");

  private static readonly Logger SpectateLog = BublockLog.For("Spectate");

  private static readonly Dictionary<ulong, CamState> States = [];

  private static readonly Dictionary<ulong, DateTime> SpawnedAt = [];

  private sealed class CamState
  {
    public bool Auto = true;
    public ulong? LastFollowed;
    public bool FallbackSent;
    public ulong? PendingKiller;
    public bool Parked;
    public RiftSide? ParkedSide;
    public DateTime? OverviewEnd;
    public ulong? ReturnTo;
    public int? LastShownRound;
    public DateTime? LastParkAt;
    public DateTime? SeatedAt;
    public DateTime? ManualUntil;
    public Vector3? ManualPosition;
    public DateTime? LastClientCommandAt;
  }

  public static void Tick(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var now = DateTime.UtcNow;

    foreach (var admin in SeatedObservers())
    {
      var state = State(admin.PlayerSteamId);

      if (!state.Auto || (state.SeatedAt is { } seated && now - seated < SeatGrace) || Held(admin, state, now, mode))
        continue;

      Update(admin, state, timer, mode);
    }
  }

  public static void Seated(ulong steamId) => State(steamId).SeatedAt = DateTime.UtcNow;

  public static void NoteSpawn(ulong steamId) => SpawnedAt[steamId] = DateTime.UtcNow;

  // Every console command from a seated admin is logged; a spectator command means they took the camera.
  public static void OnAdminCommand(CCitadelPlayerController admin, string command, string[] args, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!AdminSeat.IsSeated(admin.PlayerSteamId))
      return;

    SpectateLog.WithMode(mode).Info(admin.ToPlayerRef(), "Admin client command Command={Command} Args={Args}", command, string.Join(" ", args));

    if (!command.StartsWith(SpectatorCommandPrefix, StringComparison.OrdinalIgnoreCase))
      return;

    var state = State(admin.PlayerSteamId);
    var now = DateTime.UtcNow;

    if (state.LastClientCommandAt is { } sent && now - sent < OwnCommandEcho)
      return;

    if (state.SeatedAt is { } seated && now - seated < SeatGrace)
      return;

    StartManual(admin, state, "command", now, mode);
  }

  public static void OnDeath(CCitadelPlayerController victim, CCitadelPlayerController? attacker, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var victimId = victim.PlayerSteamId;
    var killerId = attacker != null && attacker.PlayerSteamId != victimId ? attacker.PlayerSteamId : (ulong?)null;
    var watched = false;

    foreach (var admin in SeatedObservers())
    {
      var state = State(admin.PlayerSteamId);

      if (!state.Auto || (state.LastFollowed != victimId && state.ReturnTo != victimId))
        continue;

      state.PendingKiller = killerId;
      watched = true;
      Log.WithMode(mode).Debug(admin.ToPlayerRef(), "Stream camera target died Victim={Victim} Killer={Killer}", victim.PlayerName, attacker?.PlayerName ?? "none");
    }

    if (watched)
      timer.NextTick(() => Tick(timer, mode));
  }

  public static void OnBigUlt(CCitadelPlayerController caster, string abilityName, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var now = DateTime.UtcNow;

    foreach (var admin in SeatedObservers())
    {
      var state = State(admin.PlayerSteamId);

      if (!state.Auto || SpectateRule.ManualActive(state.ManualUntil, now))
        continue;

      if (OverviewRule.Showing(state.OverviewEnd, now))
      {
        state.ReturnTo = caster.PlayerSteamId;
        log.Info(admin.ToPlayerRef(), "Big ult during top-down, returning to the newest caster Caster={Caster} Ability={Ability}", caster.PlayerName, abilityName);
        continue;
      }

      if (!OverviewRule.CanStart(RiftService.IsRunning, RiftService.RoundNumber, state.LastShownRound))
      {
        log.Info(admin.ToPlayerRef(), "Big ult skipped, top-down already shown this round or no live round Caster={Caster} Ability={Ability} Round={Round}", caster.PlayerName, abilityName, RiftService.RoundNumber);
        continue;
      }

      state.LastShownRound = RiftService.RoundNumber;
      StartOverview(admin, state, caster.PlayerSteamId, timer, mode);
      log.Info(admin.ToPlayerRef(), "Stream camera Reason={Reason} Caster={Caster} Ability={Ability} Round={Round}", "ult", caster.PlayerName, abilityName, RiftService.RoundNumber);
    }
  }

  public static string ShowOverview(CCitadelPlayerController admin, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!SpectateService.IsObserving(admin))
      throw new CommandException("Not spectating; use dw_seat_spec first.");

    StartOverview(admin, State(admin.PlayerSteamId), null, timer, mode);
    Log.WithMode(mode).Info(admin.ToPlayerRef(), "Stream camera Reason={Reason}", "overview");
    return $"Top-down view for {OverviewRule.Duration.TotalSeconds:0} s.";
  }

  public static bool SetAuto(CCitadelPlayerController admin, bool on, ExecutionMode mode = ExecutionMode.Clean)
  {
    var state = State(admin.PlayerSteamId);
    var changed = state.Auto != on;

    state.Auto = on;
    state.OverviewEnd = null;
    state.Parked = false;
    state.ManualUntil = null;
    Log.WithMode(mode).Info(admin.ToPlayerRef(), "Stream camera auto Auto={Auto}", on);
    return changed;
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
    var showing = OverviewRule.Showing(state.OverviewEnd, DateTime.UtcNow);
    var roundUsed = state.LastShownRound != null && state.LastShownRound == RiftService.RoundNumber;
    var manual = SpectateRule.ManualActive(state.ManualUntil, DateTime.UtcNow) ? $"until {state.ManualUntil:HH:mm:ss} UTC" : "off";

    return
    [
      $"Auto={state.Auto} | Manual={manual} | Seated={AdminSeat.IsSeated(admin.PlayerSteamId)} | Observer={SpectateService.IsObserving(admin)} | Mode={SpectateService.Mode(admin)}",
      $"Watching={watching?.PlayerName ?? (target == null ? "none" : target.DesignerName)} | Parked={state.Parked} Side={(state.ParkedSide is { } side ? RiftSides.Name(side) : "-")}",
      $"TopDown={(showing ? "showing" : "off")} | ReturnTo={NameOf(state.ReturnTo)} | Round={RiftService.RoundNumber} Live={RiftService.IsRunning} RoundTopDownUsed={roundUsed}"
    ];
  }

  private static void Update(CCitadelPlayerController admin, CamState state, ITimer timer, ExecutionMode mode)
  {
    var now = DateTime.UtcNow;
    var returning = false;

    if (state.OverviewEnd != null)
    {
      if (OverviewRule.Showing(state.OverviewEnd, now))
        return;

      state.OverviewEnd = null;
      returning = true;
    }

    var candidates = Candidates();
    var ids = candidates.Select(player => player.PlayerSteamId).ToList();

    if (returning && state.ReturnTo is { } caster && ids.Contains(caster))
    {
      state.ReturnTo = null;
      Follow(admin, state, candidates.First(player => player.PlayerSteamId == caster), "caster", mode);
      return;
    }

    state.ReturnTo = null;

    if (!returning && LeftFollow(admin, state, candidates))
    {
      StartManual(admin, state, "left-follow", now, mode);
      return;
    }

    var currentId = returning ? null : CurrentId(admin, candidates) ?? PendingFollow(admin, state, candidates, mode);
    var choice = SpectateRule.Choose(currentId, state.PendingKiller, ids);

    switch (choice.Reason)
    {
      case SpectateReason.Keep:
        if (state.LastFollowed != choice.Target)
        {
          Log.WithMode(mode).Info(admin.ToPlayerRef(), "Stream camera Reason={Reason} Target={Target}", "keep", NameOf(choice.Target));
          state.FallbackSent = false;
        }

        state.LastFollowed = choice.Target;
        state.PendingKiller = null;
        state.Parked = false;
        break;

      case SpectateReason.Killer:
      case SpectateReason.Any:
        var reason = choice.Reason == SpectateReason.Killer ? "killer" : "any";
        Follow(admin, state, candidates.First(player => player.PlayerSteamId == choice.Target), reason, mode);
        break;

      default:
        Park(admin, state, timer, mode);
        break;
    }
  }

  // The server target may not stick; the client command is sent once per target before giving up on it.
  private static ulong? PendingFollow(CCitadelPlayerController admin, CamState state, List<CCitadelPlayerController> candidates, ExecutionMode mode)
  {
    if (state.LastFollowed is not { } followed)
      return null;

    var target = candidates.FirstOrDefault(player => player.PlayerSteamId == followed);

    if (target == null)
      return null;

    if (!state.FallbackSent)
    {
      state.LastClientCommandAt = DateTime.UtcNow;
      SpectateService.ClientFollow(admin, target, mode);
      state.FallbackSent = true;
    }

    return followed;
  }

  private static void Follow(CCitadelPlayerController admin, CamState state, CCitadelPlayerController target, string reason, ExecutionMode mode)
  {
    state.LastClientCommandAt = DateTime.UtcNow;
    var accepted = SpectateService.Follow(admin, target, mode);

    state.LastFollowed = target.PlayerSteamId;
    state.FallbackSent = !accepted;
    state.PendingKiller = null;
    state.Parked = false;

    Log.WithMode(mode).Info(admin.ToPlayerRef(), "Stream camera Reason={Reason} Target={Target} Accepted={Accepted}", reason, target.PlayerName, accepted);
  }

  // Parked is only a claim: the client can leave fly cam (or never enter it), so a park that did not hold is retried.
  private static void Park(CCitadelPlayerController admin, CamState state, ITimer timer, ExecutionMode mode)
  {
    var side = WatchSpot.Side;
    var now = DateTime.UtcNow;
    var reason = "park";
    state.LastFollowed = null;
    state.PendingKiller = null;

    if (state.Parked && state.ParkedSide == side)
    {
      if (state.LastParkAt is { } last && now - last < ReparkEvery)
        return;

      if (SpectateService.IsParkedAt(admin, OverheadSpot(side)))
        return;

      if (SpectateService.IsManualMove(admin, OverheadSpot(side)))
      {
        StartManual(admin, state, "moved", now, mode);
        return;
      }

      reason = "repark";
    }

    if (ParkOverhead(admin, side, timer, mode))
    {
      state.Parked = true;
      state.ParkedSide = side;
      state.LastParkAt = now;
      Log.WithMode(mode).Info(admin.ToPlayerRef(), "Stream camera Reason={Reason} Side={Side} Mode={Mode}", reason, RiftSides.Name(side), SpectateService.Mode(admin));
    }
  }

  private static void StartOverview(CCitadelPlayerController admin, CamState state, ulong? returnTo, ITimer timer, ExecutionMode mode)
  {
    ParkOverhead(admin, WatchSpot.Side, timer, mode);

    state.OverviewEnd = OverviewRule.EndFrom(DateTime.UtcNow);
    state.ReturnTo = returnTo ?? state.LastFollowed;
    state.LastFollowed = null;
    state.Parked = false;

    timer.Once(OverviewRule.Duration.TotalSeconds.Seconds(), () => Tick(timer, mode));
  }

  private static bool ParkOverhead(CCitadelPlayerController admin, RiftSide side, ITimer timer, ExecutionMode mode)
  {
    State(admin.PlayerSteamId).LastClientCommandAt = DateTime.UtcNow;
    return SpectateService.Park(admin, OverheadSpot(side), SpectateRule.LookDown(WatchSpot.Location(side).Angle.Y), timer, mode);
  }

  // The camera was following a player who is still alive, but the viewer is now in fly cam with no target.
  private static bool LeftFollow(CCitadelPlayerController admin, CamState state, List<CCitadelPlayerController> candidates) =>
    state.LastFollowed is { } followed &&
    candidates.Any(player => player.PlayerSteamId == followed) &&
    SpectateService.Mode(admin) == ObserverMode_t.Roaming &&
    SpectateService.Current(admin) == null;

  private static void StartManual(CCitadelPlayerController admin, CamState state, string reason, DateTime now, ExecutionMode mode)
  {
    var wasHeld = SpectateRule.ManualActive(state.ManualUntil, now);

    state.ManualUntil = now + ManualHold;
    state.ManualPosition = SpectateService.Observer(admin)?.Position;
    state.OverviewEnd = null;
    state.ReturnTo = null;
    state.PendingKiller = null;
    state.LastFollowed = null;
    state.Parked = false;

    if (!wasHeld)
      Log.WithMode(mode).Info(admin.ToPlayerRef(), "Stream camera paused, manual control Reason={Reason} HoldSeconds={HoldSeconds}", reason, ManualHold.TotalSeconds);
  }

  // While held, any camera movement extends the hold; the camera resumes after ManualHold of stillness.
  private static bool Held(CCitadelPlayerController admin, CamState state, DateTime now, ExecutionMode mode)
  {
    if (state.ManualUntil == null)
      return false;

    var position = SpectateService.Observer(admin)?.Position;

    if (position is { } current && state.ManualPosition is { } last && Vector3.Distance(current, last) > ManualMoveUnits)
    {
      state.ManualUntil = now + ManualHold;
      state.ManualPosition = current;
    }

    if (SpectateRule.ManualActive(state.ManualUntil, now))
      return true;

    state.ManualUntil = null;
    state.ManualPosition = null;
    Log.WithMode(mode).Info(admin.ToPlayerRef(), "Stream camera resumed after manual control");
    return false;
  }

  private static Vector3 OverheadSpot(RiftSide side) =>
    WatchSpot.Location(side).Position + new Vector3(0f, 0f, OverheadHeight);

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
