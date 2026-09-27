using System.Globalization;
using Bublock.Modules.Hud;
using Bublock.Modules.Loadout;
using Bublock.Modules.Queue;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Balance;
using RiftRoulette.Draft;
using RiftRoulette.GameLoop;
using RiftRoulette.Lobby;
using RiftRoulette.Stats;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.RandomMode;

public sealed record RandomAssignment(Heroes Hero, HeroBuild Build, int Team);

public static class RandomModeService
{
  public const double JoinerFallbackSeconds = 2.0;

  public static readonly LoadoutOptions Options = new(Gold: 0);

  private static readonly Logger Log = BublockLog.For("Random");

  private static readonly Dictionary<ulong, int> Teams = [];

  private static readonly Dictionary<ulong, Heroes> LastHero = [];

  private static readonly Dictionary<ulong, RandomAssignment> Assignments = [];

  private static readonly Dictionary<ulong, int> Values = [];

  private static readonly HeroLock Lock = new();

  private static readonly PlayerQueue BenchRotation = new();

  private static ulong? _benched;

  private static ulong? _returning;

  private static int? _benchRound;

  private static bool _buildsAnnounced;

  public const string SitOutTitle = "Sitting out";
  public const string SitOutDescription = "You play next round";

  public static int PendingCount => Lock.PendingCount;

  public static ulong? Benched => _benched;

  public static void BeginMatch(ExecutionMode mode = ExecutionMode.Clean)
  {
    Clear();

    var current = Humans().ToDictionary(player => player.PlayerSteamId, player => player.TeamNum);

    foreach (var (steamId, team) in TeamBalance.Even(current, Random.Shared))
      Teams[steamId] = team;

    Log.WithMode(mode).Info(
      "Teams balanced Sapphire={Sapphire} Amber={Amber} Moved={Moved}",
      Teams.Values.Count(team => team == RiftRouletteTeams.Sapphire),
      Teams.Values.Count(team => team == RiftRouletteTeams.Amber),
      Teams.Count(pair => current[pair.Key] != pair.Value));
  }

  public static int PrepareRound(ITimer timer, ExecutionMode mode = ExecutionMode.Clean, bool forceBalance = false)
  {
    var log = Log.WithMode(mode);
    var catalog = HeroBuildCatalog.Default;
    var players = Humans();
    var connected = players.Select(player => player.PlayerSteamId).ToHashSet();

    foreach (var gone in Teams.Keys.Where(steamId => !connected.Contains(steamId)).ToList())
      Teams.Remove(gone);

    foreach (var player in players.Where(player => !Teams.ContainsKey(player.PlayerSteamId)))
    {
      Teams[player.PlayerSteamId] = TeamBalance.SmallerTeam(Teams.Values, Random.Shared);
      log.Info(player.ToPlayerRef(), "Late joiner placed Team={Team}", RiftRouletteTeams.Name(Teams[player.PlayerSteamId]));
    }

    // A reroll in the same intermission keeps the bench; only a new intermission rotates it.
    if (_benchRound != MatchService.State.Round)
    {
      _returning = _benched;
      _benched = BenchRule.Next(BenchRotation, connected);
      _benchRound = MatchService.State.Round;
    }
    else if (_benched is { } kept && !connected.Contains(kept))
    {
      _benched = null;
    }

    var fighters = new Dictionary<ulong, int>(
      BenchRule.FightingTeams(Teams, _benched, _returning == _benched ? null : _returning, Random.Shared));

    LogEvened(log, fighters);
    BalanceService.TryBalance(fighters, mode, forceBalance);

    foreach (var (steamId, team) in fighters)
      Teams[steamId] = team;

    if (_benched is { } benchedId && Find(benchedId) is { } benchedPlayer)
      log.Info(benchedPlayer.ToPlayerRef(), "Sitting out this round Players={Players}", connected.Count);

    var heroes = HeroDraw.Draw(fighters.Keys.ToList(), catalog.Heroes, LastHero, Random.Shared);

    Assignments.Clear();
    Values.Clear();
    _buildsAnnounced = false;
    Lock.ClearAllPending();
    DraftState.Clear();

    var swapped = 0;

    foreach (var player in players)
    {
      if (!heroes.TryGetValue(player.PlayerSteamId, out var hero))
        continue;

      if (Start(player, Assign(player.PlayerSteamId, hero, catalog), timer, mode))
        swapped++;
      else
        Lock.MarkPending(player.PlayerSteamId);
    }

    log.Info("Round prepared Players={Players} Swapped={Swapped} Pending={Pending}", Assignments.Count, swapped, Lock.PendingCount);
    StatsService.RefreshBoards(mode);
    return swapped;
  }

  private static void LogEvened(Logger log, IReadOnlyDictionary<ulong, int> fighters)
  {
    var moved = fighters.Count(pair => Teams.TryGetValue(pair.Key, out var team) && team != pair.Value);

    if (moved == 0)
      return;

    log.Info(
      "Teams evened Moved={Moved} Sapphire={Sapphire} Amber={Amber}",
      moved,
      fighters.Values.Count(team => team == RiftRouletteTeams.Sapphire),
      fighters.Values.Count(team => team == RiftRouletteTeams.Amber));
  }

  // During an intermission the fighters stay even: a joiner fills an odd gap, swaps in with the bench player, or sits out.
  public static void AddJoiner(CCitadelPlayerController player, int team, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var steamId = player.PlayerSteamId;

    Teams[steamId] = team;
    BenchRotation.Join(steamId);

    if (MatchService.State.Phase != MatchPhase.Intermission)
    {
      log.Info(player.ToPlayerRef(), "Joiner placed, hero next intermission Team={Team}", RiftRouletteTeams.Name(team));
      return;
    }

    var fighterTeams = FighterTeams();

    if (fighterTeams.Count % 2 == 1)
    {
      Teams[steamId] = TeamBalance.SmallerTeam(fighterTeams, Random.Shared);
      AssignLate(player, timer, mode);
      return;
    }

    if (_benched is { } benchedId && benchedId != steamId && Find(benchedId) is { } benched)
    {
      _benched = null;
      Teams[benchedId] = RiftRouletteTeams.Other(team);
      AssignLate(player, timer, mode);
      AssignLate(benched, timer, mode);
      ApplyPending(benched, timer, mode);
      log.Info(benched.ToPlayerRef(), "Subbed in with a joiner Joiner={Joiner} Team={Team}", player.PlayerName, RiftRouletteTeams.Name(Teams[benchedId]));
      return;
    }

    _benched = steamId;
    log.Info(player.ToPlayerRef(), "Joiner sitting out this round Team={Team}", RiftRouletteTeams.Name(team));

    if (_buildsAnnounced)
      HudService.Announce(player, SitOutTitle, SitOutDescription, mode);
  }

  // Called before the leaver is removed. In an intermission the bench player takes the leaver's team.
  public static void OnLeave(ulong steamId, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var wasFighter = Assignments.ContainsKey(steamId);
    var hadTeam = Teams.TryGetValue(steamId, out var team);

    Forget(steamId);

    if (!wasFighter || !hadTeam || MatchService.State.Phase != MatchPhase.Intermission)
      return;

    if (_benched is not { } benchedId || Find(benchedId) is not { } benched)
      return;

    _benched = null;
    Teams[benchedId] = team;
    AssignLate(benched, timer, mode);
    ApplyPending(benched, timer, mode);
    log.Info(benched.ToPlayerRef(), "Subbed in for a player who left Team={Team}", RiftRouletteTeams.Name(team));
  }

  private static void AssignLate(CCitadelPlayerController player, ITimer timer, ExecutionMode mode)
  {
    var steamId = player.PlayerSteamId;
    var catalog = HeroBuildCatalog.Default;
    var taken = Assignments.Values.Select(assignment => assignment.Hero).ToHashSet();
    var free = catalog.Heroes.Where(hero => !taken.Contains(hero)).ToList();
    var pool = free.Count > 0 ? free : catalog.Heroes;

    if (pool.Count == 0)
      return;

    var hero = HeroDraw.Draw([steamId], pool, LastHero, Random.Shared)[steamId];
    Assign(steamId, hero, catalog);

    Lock.MarkPending(steamId);
    Log.WithMode(mode).Info(player.ToPlayerRef(), "Late hero assigned, pending spawn Hero={Hero} Team={Team}", hero, RiftRouletteTeams.Name(Teams[steamId]));

    timer.Once(JoinerFallbackSeconds.Seconds(), () =>
    {
      if (Find(steamId) is { } current)
        ApplyPending(current, timer, mode);
    });
  }

  private static List<int> FighterTeams()
  {
    var connected = Humans().Select(player => player.PlayerSteamId).ToHashSet();
    return Assignments.Where(pair => connected.Contains(pair.Key)).Select(pair => pair.Value.Team).ToList();
  }

  private static CCitadelPlayerController? Find(ulong steamId) =>
    Players.GetAll().FirstOrDefault(player => player.PlayerSteamId == steamId);

  public static bool ApplyPending(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;

    if (!MatchConfig.IsRandom || !Lock.IsPending(steamId) || !Assignments.TryGetValue(steamId, out var assignment))
      return false;

    if (!Start(player, assignment, timer, mode))
      return false;

    Lock.ClearPending(steamId);
    Log.WithMode(mode).Info(player.ToPlayerRef(), "Pending loadout started after spawn Hero={Hero}", assignment.Hero);
    return true;
  }

  public static bool TryGetAssignment(ulong steamId, out RandomAssignment assignment) =>
    Assignments.TryGetValue(steamId, out assignment!);

  public static bool GuardHero(
    CCitadelPlayerController player,
    CCitadelPlayerPawn pawn,
    ITimer timer,
    ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;

    if (!MatchConfig.IsRandom || !Assignments.TryGetValue(steamId, out var assignment))
      return false;

    Lock.Enforce(
      player,
      pawn,
      assignment.Hero,
      HeroBuildCatalog.Default.DisplayName(assignment.Hero),
      Log.WithMode(mode),
      () => Start(player, assignment, timer, mode));

    return true;
  }

  public static bool ConsumeEnforcementKill(ulong steamId) => Lock.ConsumeKill(steamId);

  public static void Forget(ulong steamId)
  {
    Teams.Remove(steamId);
    Assignments.Remove(steamId);
    Values.Remove(steamId);
    Lock.Forget(steamId);
    BenchRotation.Leave(steamId);

    if (_benched == steamId)
      _benched = null;
  }

  public static int AnnounceBuilds(ExecutionMode mode = ExecutionMode.Clean)
  {
    _buildsAnnounced = true;
    var shown = 0;

    foreach (var player in Players.GetAll())
    {
      if (AnnounceBuild(player, mode))
      {
        shown++;
      }
      else if (_benched == player.PlayerSteamId)
      {
        HudService.Announce(player, SitOutTitle, SitOutDescription, mode);
        shown++;
      }
    }

    return shown;
  }

  public static string BuildDescription(string buildName, int souls) =>
    $"{buildName} - {souls.ToString("N0", CultureInfo.InvariantCulture)} souls";

  private static bool AnnounceBuild(CCitadelPlayerController player, ExecutionMode mode)
  {
    var steamId = player.PlayerSteamId;

    if (!Assignments.TryGetValue(steamId, out var assignment) || !Values.TryGetValue(steamId, out var souls))
      return false;

    HudService.Announce(
      player,
      HeroBuildCatalog.Default.DisplayName(assignment.Hero),
      BuildDescription(assignment.Build.Name, souls),
      mode);

    return true;
  }

  public static int EndMatch(ExecutionMode mode = ExecutionMode.Clean)
  {
    var reset = 0;

    foreach (var player in Players.GetAll())
    {
      var pawn = player.GetHeroPawn();

      if (pawn == null || !pawn.IsAlive || !Assignments.ContainsKey(player.PlayerSteamId))
        continue;

      pawn.ResetHero();
      reset++;
    }

    Log.WithMode(mode).Info("Random match ended HeroesReset={HeroesReset}", reset);
    Clear();
    return reset;
  }

  public static IReadOnlyList<string> Describe()
  {
    var catalog = HeroBuildCatalog.Default;
    var lines = new List<string>
    {
      $"{MatchConfig.Describe()} | Assigned={Assignments.Count} | Pending={Lock.PendingCount} | Teams={Teams.Count} | " +
      $"Bench={(_benched is { } benched ? Find(benched)?.PlayerName ?? benched.ToString() : "none")}"
    };

    foreach (var player in Players.GetAll())
    {
      var steamId = player.PlayerSteamId;
      var team = Teams.TryGetValue(steamId, out var number) ? RiftRouletteTeams.Name(number) : "-";
      var hero = Assignments.TryGetValue(steamId, out var assignment)
        ? $"{catalog.DisplayName(assignment.Hero)} | Build={assignment.Build.Name} ({assignment.Build.BuildId})"
        : "-";
      var pending = Lock.IsPending(steamId) ? " | PENDING" : "";
      var sitting = _benched == steamId ? " | SITTING OUT" : "";

      lines.Add($"Slot={player.Slot} | {player.PlayerName} | Team={team} | Hero={hero}{pending}{sitting}");
    }

    return lines;
  }

  private static RandomAssignment Assign(ulong steamId, Heroes hero, HeroBuildCatalog catalog)
  {
    var builds = catalog.BuildsFor(hero);
    var assignment = new RandomAssignment(hero, builds[Random.Shared.Next(builds.Count)], Teams[steamId]);

    Assignments[steamId] = assignment;
    LastHero[steamId] = hero;
    Lock.Unapply(steamId);

    if (!DraftState.IsSelected(hero))
      DraftState.Add(steamId, hero);

    return assignment;
  }

  private static bool Start(CCitadelPlayerController player, RandomAssignment assignment, ITimer timer, ExecutionMode mode)
  {
    var pawn = player.GetHeroPawn();

    if (pawn == null || !pawn.IsAlive)
    {
      Log.WithMode(mode).Debug(player.ToPlayerRef(), "Swap deferred, player dead Hero={Hero}", assignment.Hero);
      return false;
    }

    if (player.TeamNum != assignment.Team)
      player.ChangeTeam(assignment.Team);

    var steamId = player.PlayerSteamId;
    Lock.Unapply(steamId);

    return LoadoutService.Swap(
      player,
      assignment.Hero,
      assignment.Build,
      timer,
      Options,
      mode,
      (current, result) =>
      {
        if (!Assignments.TryGetValue(steamId, out var live) || live != assignment)
          return;

        Lock.MarkApplied(steamId);
        Values[steamId] = result.Value;

        if (_buildsAnnounced)
          AnnounceBuild(current, mode);
      });
  }

  private static List<CCitadelPlayerController> Humans() => Participants.Humans();

  private static void Clear()
  {
    Teams.Clear();
    LastHero.Clear();
    Assignments.Clear();
    Values.Clear();
    _buildsAnnounced = false;
    Lock.Clear();
    BenchRotation.Clear();
    _benched = null;
    _returning = null;
    _benchRound = null;
  }
}
