using System.Globalization;
using Bublock.Modules.Hud;
using Bublock.Modules.Loadout;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.GameLoop;
using RiftRoulette.RandomMode;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.GunGame;

public static class GunGameService
{
  public const int RestartDelaySeconds = 10;

  public const string Title = "Gun Game";

  private static readonly Logger Log = BublockLog.For("GunGame");

  private static readonly Dictionary<ulong, string> Names = [];

  private static ITimer? _timer;

  public static GunGameLadder Ladder { get; } = new();

  public static bool Active => MatchConfig.IsGunGame && MatchService.State.IsRunning;

  public static void BeginMatch(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    _timer = timer;
    Ladder.Reset();
    Names.Clear();

    Log.WithMode(mode).Info("Gun Game started Target={Target}", Ladder.Target);
  }

  public static void EndMatch(ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);

    foreach (var (steamId, kills) in Ladder.Standings())
      log.Info("Final ladder Player={Player} Steam={Steam} Kills={Kills}", NameOf(steamId), steamId, kills);

    log.Info("Gun Game ended Winner={Winner} Target={Target}", Ladder.Winner is { } winner ? NameOf(winner) : "-", Ladder.Target);
    Ladder.Reset();
    Names.Clear();
    _timer = null;
  }

  // From StatsService.RecordDeath once a kill is credited (human attacker, enemy victim, not a hero lock kill).
  public static void OnKill(CCitadelPlayerController attacker, CCitadelPlayerController victim, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!Active || _timer is not { } timer)
      return;

    var log = Log.WithMode(mode);
    var step = Ladder.Record(attacker.PlayerSteamId, victim.PlayerSteamId);

    if (!step.Counted)
      return;

    Names[attacker.PlayerSteamId] = attacker.PlayerName;
    log.Info(attacker.ToPlayerRef(), "Kill counted Kills={Kills} Target={Target} Victim={Victim}", step.Kills, Ladder.Target, victim.PlayerName);

    if (step.Won)
    {
      Win(attacker, timer, mode);
      return;
    }

    var kills = step.Kills;

    if (RandomModeService.Reroll(attacker, timer, mode, (current, assignment, souls) => AnnounceLevel(current, kills, assignment, souls, mode)) == null)
      log.Warn(attacker.ToPlayerRef(), "Kill counted but no reroll (no Random assignment) Kills={Kills}", kills);
  }

  // Admin test path: the same reroll a kill gives, without a ladder step.
  public static string Reroll(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!MatchService.State.IsRunning || !MatchConfig.IsRandom)
      return "Reroll needs a running Random mode match.";

    var assignment = RandomModeService.Reroll(
      player,
      timer,
      mode,
      (current, landed, souls) => HudService.Announce(
        current,
        HeroBuildCatalog.Default.DisplayName(landed.Hero),
        RandomModeService.BuildDescription(landed.Build.Name, souls),
        mode));

    return assignment == null
      ? $"{player.PlayerName} has no Random mode hero this round (sitting out or just joined)."
      : $"{player.PlayerName} -> {HeroBuildCatalog.Default.DisplayName(assignment.Hero)} ({assignment.Build.Name}).";
  }

  public static string SetTarget(int kills, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (MatchService.State.IsRunning)
      return "A match is running. Set the target between matches.";

    if (!Ladder.TrySetTarget(kills))
      return $"Target must be {GunGameLadder.MinTarget} to {GunGameLadder.MaxTarget} kills.";

    Log.WithMode(mode).Info("Gun Game target set Target={Target}", Ladder.Target);
    return $"Gun Game target set to {Ladder.Target} kills.";
  }

  public static string StartDescription(int intermissionSeconds) =>
    $"First to {Ladder.Target} kills - round 1 in {intermissionSeconds}s";

  // The final score in MatchService.End's "Match over" banner.
  public static string Summary()
  {
    if (Ladder.Winner is { } winner)
      return $"{NameOf(winner)} wins - {Ladder.Target} kills";

    return Ladder.Standings() is [var (leader, kills), ..]
      ? $"Leader: {NameOf(leader)} - {kills}/{Ladder.Target} kills"
      : $"No kills yet - first to {Ladder.Target}";
  }

  public static IReadOnlyList<string> Describe()
  {
    List<string> lines =
    [
      $"Active={Active} | {MatchConfig.Describe()} | Target={Ladder.Target} | Winner={(Ladder.Winner is { } winner ? NameOf(winner) : "-")}"
    ];

    var place = 1;

    foreach (var (steamId, kills) in Ladder.Standings())
      lines.Add($"{place++}. {NameOf(steamId)} ({steamId}) {kills}/{Ladder.Target}");

    if (Ladder.Kills.Count == 0)
      lines.Add("No kills yet.");

    return lines;
  }

  public static IReadOnlyList<string> DescribePlayer(CCitadelPlayerController player)
  {
    if (!Active)
      return ["Gun Game is not running (/match_format gungame in Random mode)."];

    var steamId = player.PlayerSteamId;
    List<string> lines = [$"You: {Ladder.KillsOf(steamId)}/{Ladder.Target} kills, place {Ladder.PlaceOf(steamId)}"];

    foreach (var (id, kills) in Ladder.Standings().Take(3))
      lines.Add($"{Ladder.PlaceOf(id)}  {NameOf(id)}  {kills}");

    return lines;
  }

  public static void Forget(ulong steamId) => Ladder.Forget(steamId);

  private static void Win(CCitadelPlayerController winner, ITimer timer, ExecutionMode mode)
  {
    Log.WithMode(mode).Info(winner.ToPlayerRef(), "Gun Game won Kills={Kills}", Ladder.Target);
    BublockLog.Master.Info(winner.ToPlayerRef(), "Gun Game won Kills={Kills}", Ladder.Target);

    // Outside the death event: MatchService.End moves everyone up top. Its "Match over" banner carries Summary().
    timer.NextTick(() =>
    {
      MatchService.End(timer, mode);
      timer.Once(RestartDelaySeconds.Seconds(), () => AutoStartService.Check(timer, mode));
    });
  }

  private static void AnnounceLevel(CCitadelPlayerController player, int kills, RandomAssignment assignment, int souls, ExecutionMode mode) =>
    HudService.Announce(
      player,
      $"Kill {kills.ToString(CultureInfo.InvariantCulture)}/{Ladder.Target}: {HeroBuildCatalog.Default.DisplayName(assignment.Hero)}",
      RandomModeService.BuildDescription(assignment.Build.Name, souls),
      mode);

  private static string NameOf(ulong steamId) =>
    Names.TryGetValue(steamId, out var name)
      ? name
      : Players.GetAll().FirstOrDefault(player => player.PlayerSteamId == steamId)?.PlayerName ?? steamId.ToString(CultureInfo.InvariantCulture);
}
