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

  private static readonly Logger Log = BublockLog.For("Lobby");

  private static readonly Dictionary<ulong, CamState> States = [];

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
  }

  public static void Tick() => Tick(ExecutionMode.Clean);

  public static void Tick(ExecutionMode mode)
  {
    foreach (var admin in SeatedObservers())
    {
      var state = State(admin.PlayerSteamId);

      if (state.Auto)
        Update(admin, state, mode);
    }
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
      timer.NextTick(() => Tick(mode));
  }

  public static void OnBigUlt(CCitadelPlayerController caster, string abilityName, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var now = DateTime.UtcNow;

    foreach (var admin in SeatedObservers())
    {
      var state = State(admin.PlayerSteamId);

      if (!state.Auto)
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
    Log.WithMode(mode).Info(admin.ToPlayerRef(), "Stream camera auto Auto={Auto}", on);
    return changed;
  }

  public static void Forget(ulong steamId)
  {
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

    return
    [
      $"Auto={state.Auto} | Seated={AdminSeat.IsSeated(admin.PlayerSteamId)} | Observer={SpectateService.IsObserving(admin)} | Mode={SpectateService.Mode(admin)}",
      $"Watching={watching?.PlayerName ?? (target == null ? "none" : target.DesignerName)} | Parked={state.Parked} Side={(state.ParkedSide is { } side ? RiftSides.Name(side) : "-")}",
      $"TopDown={(showing ? "showing" : "off")} | ReturnTo={NameOf(state.ReturnTo)} | Round={RiftService.RoundNumber} Live={RiftService.IsRunning} RoundTopDownUsed={roundUsed}"
    ];
  }

  private static void Update(CCitadelPlayerController admin, CamState state, ExecutionMode mode)
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
        Park(admin, state, mode);
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
      SpectateService.ClientFollow(admin, target, mode);
      state.FallbackSent = true;
    }

    return followed;
  }

  private static void Follow(CCitadelPlayerController admin, CamState state, CCitadelPlayerController target, string reason, ExecutionMode mode)
  {
    var accepted = SpectateService.Follow(admin, target, mode);

    state.LastFollowed = target.PlayerSteamId;
    state.FallbackSent = !accepted;
    state.PendingKiller = null;
    state.Parked = false;

    Log.WithMode(mode).Info(admin.ToPlayerRef(), "Stream camera Reason={Reason} Target={Target} Accepted={Accepted}", reason, target.PlayerName, accepted);
  }

  private static void Park(CCitadelPlayerController admin, CamState state, ExecutionMode mode)
  {
    var side = WatchSpot.Side;
    state.LastFollowed = null;
    state.PendingKiller = null;

    if (state.Parked && state.ParkedSide == side)
      return;

    if (ParkOverhead(admin, side, mode))
    {
      state.Parked = true;
      state.ParkedSide = side;
      Log.WithMode(mode).Info(admin.ToPlayerRef(), "Stream camera Reason={Reason} Side={Side}", "park", RiftSides.Name(side));
    }
  }

  private static void StartOverview(CCitadelPlayerController admin, CamState state, ulong? returnTo, ITimer timer, ExecutionMode mode)
  {
    ParkOverhead(admin, WatchSpot.Side, mode);

    state.OverviewEnd = OverviewRule.EndFrom(DateTime.UtcNow);
    state.ReturnTo = returnTo ?? state.LastFollowed;
    state.LastFollowed = null;
    state.Parked = false;

    timer.Once(OverviewRule.Duration.TotalSeconds.Seconds(), () => Tick(mode));
  }

  private static bool ParkOverhead(CCitadelPlayerController admin, RiftSide side, ExecutionMode mode)
  {
    var location = WatchSpot.Location(side);
    return SpectateService.Park(admin, location.Position, SpectateRule.LookDown(location.Angle.Y), mode);
  }

  // During a round, restrained players are waiting up top; the camera prefers players who can fight.
  private static List<CCitadelPlayerController> Candidates()
  {
    var live = Participants.Humans()
      .Where(player => player.GetHeroPawn() is { } pawn && pawn.IsAlive)
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
