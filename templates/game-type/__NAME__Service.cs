using Bublock.Modules.Arena;
using Bublock.Modules.Economy;
using Bublock.Modules.Hud;
using Bublock.Modules.Session;
using Bublock.Modules.Teams;
using Bublock.Shared;
using DeadworksManaged.Api;
using ITimer = DeadworksManaged.Api.ITimer;

namespace __NAME__;

// The game type: engine modules wired to __TITLE__'s rules. Plugin classes only forward hooks here.
public static class __NAME__Service
{
  public const double JoinCheckSeconds = 2.0;
  public const double ConvarDelaySeconds = 3.0;

  private static readonly Logger Log = BublockLog.For("__NAME__");

  private static readonly Dictionary<ulong, string> Names = [];

  private static readonly Lazy<ArenaSpots> LazyArena = new(() => ArenaSpots.Load(typeof(__NAME__Service).Assembly, __NAME__Rules.ArenaResource));

  public static ArenaSpots Arena => LazyArena.Value;

  public static TimedSession Session { get; } = CreateSession();

  public static void ApplyServerConvars(ExecutionMode mode = ExecutionMode.Clean)
  {
    ServerConVars.TrySet("citadel_team_size", 6, Log);
    ServerConVars.TrySet("citadel_allow_duplicate_heroes", 1, Log);
    ServerConVars.TrySet("citadel_koth_enabled", 0, Log);
    ServerConVars.TrySet("citadel_allow_purchasing_anywhere", 0, Log);
    Server.ExecuteCommand("citadel_player_override_spawn_time 1");

    if (Arena.ActiveLane is { } lane)
      ServerConVars.TrySet("citadel_active_lane", lane, Log);

    Log.WithMode(mode).Info("Server convars applied Arena={Arena}", Arena.Name);
  }

  public static void Admit(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;
    var team = DeadlockTeams.SmallerTeam(Humans().Where(other => other.PlayerSteamId != steamId).Select(other => other.TeamNum), Random.Shared);
    Names[steamId] = player.PlayerName;
    player.ChangeTeam(team, true);

    Log.WithMode(mode).Info(player.ToPlayerRef(), "Player admitted Team={Team}", DeadlockTeams.Name(team));
    timer.Once(JoinCheckSeconds.Seconds(), () => Session.Check(timer, mode));
  }

  public static void Remove(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;
    Session.Scores.Forget(steamId);
    Log.WithMode(mode).Info(player.ToPlayerRef(), "Player left");
    Session.Check(timer, mode, Humans().Count(other => other.PlayerSteamId != steamId));
  }

  // Players pick their own heroes in the scaffold; teams are the game's.
  public static bool BlocksCommand(CCitadelPlayerController? player, string command) =>
    player != null && !player.IsBot && ChoiceGuard.Blocks(command, heroLocked: false, teamLocked: true);

  public static bool BlocksCurrency(ECurrencyType type, ECurrencySource source, int amount) =>
    SoulRule.ShouldBlock(type, source, amount, active: false);

  public static void OnSpawn(CCitadelPlayerController player, ITimer timer) =>
    ArenaService.SendToArenaNextTick(player, Arena, timer);

  public static int ContainPlayers(ExecutionMode mode = ExecutionMode.Clean) =>
    ArenaService.ReturnStrays(Players.GetAll().Where(player => DeadlockTeams.IsPlayable(player.TeamNum)), Arena, mode);

  public static void OnDeath(PlayerDeathEvent args, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!Session.IsPlaying)
      return;

    var attacker = args.AttackerController?.As<CCitadelPlayerController>();
    var victim = args.UseridController?.As<CCitadelPlayerController>();

    if (attacker == null || victim == null || attacker.IsBot
        || !__NAME__Rules.Credits(attacker.PlayerSteamId, victim.PlayerSteamId, attacker.TeamNum, victim.TeamNum))
      return;

    var points = Session.Scores.Add(attacker.PlayerSteamId);
    Names[attacker.PlayerSteamId] = attacker.PlayerName;
    Log.WithMode(mode).Info(attacker.ToPlayerRef(), "Point Points={Points} Victim={Victim} Match={Match}", points, victim.PlayerName, Session.Match);
  }

  public static IReadOnlyList<string> DescribeFor(CCitadelPlayerController player)
  {
    if (!Session.IsPlaying)
      return [$"No match running ({Session.Phase}). {__NAME__Rules.WaitingLine(Humans().Count, Session.Options.MinPlayers)}"];

    var steamId = player.PlayerSteamId;
    List<string> lines =
    [
      $"Match {Session.Match} - {SessionRule.Clock(Session.SecondsLeft)} left",
      $"You: {Session.Scores.PointsOf(steamId)} points, place {Session.Scores.PlaceOf(steamId)}"
    ];

    foreach (var (id, points) in Session.Scores.Standings().Take(3))
      lines.Add($"{Session.Scores.PlaceOf(id)}  {NameOf(id)}  {points}");

    return lines;
  }

  public static IReadOnlyList<string> Describe() =>
  [
    .. Session.Describe(NameOf),
    $"Arena={Arena.Name} | ActiveLane={Arena.ActiveLane?.ToString() ?? "-"}"
  ];

  public static List<CCitadelPlayerController> Humans() =>
    Players.GetAll().Where(player => !player.IsBot).ToList();

  public static string NameOf(ulong steamId) =>
    Names.TryGetValue(steamId, out var name)
      ? name
      : Players.GetAll().FirstOrDefault(player => player.PlayerSteamId == steamId)?.PlayerName ?? steamId.ToString();

  private static TimedSession CreateSession() =>
    new(__NAME__Rules.Title, __NAME__Rules.Session, () => Humans().Count(player => DeadlockTeams.IsPlayable(player.TeamNum)))
    {
      Started = mode =>
      {
        var (title, description) = __NAME__Rules.StartBanner(Session.Options.MatchSeconds);
        HudService.AnnounceAll(title, description, mode);
      },
      Warned = (secondsLeft, mode) =>
      {
        var (title, description) = __NAME__Rules.WarningBanner(secondsLeft);
        HudService.AnnounceAll(title, description, mode);
      },
      Ended = (result, mode) =>
      {
        var (title, description) = __NAME__Rules.ResultBanner(result, NameOf, Session.Options.BreakSeconds);
        HudService.AnnounceAll(title, description, mode);
      },
      Stopped = mode =>
      {
        foreach (var player in Humans())
          PlayerChat.Send(player, __NAME__Rules.WaitingLine(Humans().Count, Session.Options.MinPlayers));
      }
    };
}
