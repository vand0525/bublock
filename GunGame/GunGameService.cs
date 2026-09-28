using Bublock.Modules.Arena;
using Bublock.Modules.Economy;
using Bublock.Modules.Hud;
using Bublock.Modules.Loadout;
using Bublock.Modules.RandomLoadout;
using Bublock.Modules.Session;
using Bublock.Modules.Teams;
using Bublock.Shared;
using DeadworksManaged.Api;
using ITimer = DeadworksManaged.Api.ITimer;

namespace GunGame;

// The whole game type: engine modules wired to Gun Game's rules. Plugin classes only forward hooks here.
public static class GunGameService
{
  public const double JoinFallbackSeconds = 2.0;
  public const double JoinCheckSeconds = 2.0;
  public const double ConvarDelaySeconds = 3.0;

  private static readonly Logger Log = BublockLog.For("GunGame");

  private static readonly Dictionary<ulong, string> Names = [];

  private static readonly Lazy<ArenaSpots> LazyArena = new(() => ArenaSpots.Load(typeof(GunGameService).Assembly, GunGameRules.ArenaResource));

  public static ArenaSpots Arena => LazyArena.Value;

  public static RandomLoadouts Heroes { get; } = CreateHeroes();

  public static TimedSession Session { get; } = CreateSession();

  // ---- lobby

  public static void ApplyServerConvars(ExecutionMode mode = ExecutionMode.Clean)
  {
    ServerConVars.TrySet("citadel_team_size", 6, Log);
    ServerConVars.TrySet("citadel_allow_duplicate_heroes", 1, Log);
    ServerConVars.TrySet("citadel_koth_enabled", 0, Log);
    ServerConVars.TrySet("citadel_allow_purchasing_anywhere", 0, Log);
    Server.ExecuteCommand("citadel_player_override_spawn_time 1");

    if (Arena.ActiveLane is { } lane)
      ServerConVars.TrySet("citadel_active_lane", lane, Log);

    Log.WithMode(mode).Info("Server convars applied Arena={Arena} ActiveLane={ActiveLane}", Arena.Name, Arena.ActiveLane?.ToString() ?? "-");
  }

  public static void Admit(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;
    var team = DeadlockTeams.SmallerTeam(Humans().Where(other => other.PlayerSteamId != steamId).Select(other => other.TeamNum), Random.Shared);
    var pick = Heroes.Draw(steamId);
    Names[steamId] = player.PlayerName;

    if (pick != null)
      player.SelectHero(pick.Hero);

    player.ChangeTeam(team, true);
    Heroes.Hold(steamId);

    Log.WithMode(mode).Info(player.ToPlayerRef(), "Player admitted Team={Team} Hero={Hero}", DeadlockTeams.Name(team), pick?.Hero.ToString() ?? "-");

    timer.Once(JoinFallbackSeconds.Seconds(), () =>
    {
      if (Find(steamId) is { } current)
        Heroes.ApplyPending(current, timer, mode);
    });

    timer.Once(JoinCheckSeconds.Seconds(), () => Session.Check(timer, mode));

    if (Session.Phase == SessionPhase.Waiting)
      PlayerChat.Send(player, GunGameRules.WaitingLine(Humans().Count, Session.Options.MinPlayers));
  }

  // After a load or hot reload: everyone already here is admitted again (statics start empty).
  public static int AdmitConnected(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var admitted = 0;

    foreach (var player in Humans())
    {
      var steamId = player.PlayerSteamId;
      Names[steamId] = player.PlayerName;

      if (!DeadlockTeams.IsPlayable(player.TeamNum))
        player.ChangeTeam(DeadlockTeams.SmallerTeam(Humans().Where(other => other.PlayerSteamId != steamId).Select(other => other.TeamNum), Random.Shared), true);

      if (Heroes.Roll(player, timer, mode) != null)
        admitted++;
    }

    Log.WithMode(mode).Info("Connected players admitted Count={Count}", admitted);
    return admitted;
  }

  public static void Remove(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;
    Heroes.Forget(steamId);
    Session.Scores.Forget(steamId);

    Log.WithMode(mode).Info(player.ToPlayerRef(), "Player left");
    Session.Check(timer, mode, Humans().Count(other => other.PlayerSteamId != steamId));
  }

  public static bool BlocksCommand(CCitadelPlayerController? player, string command) =>
    player != null
    && !player.IsBot
    && ChoiceGuard.Blocks(command, heroLocked: Heroes.TryGet(player.PlayerSteamId, out _), teamLocked: true);

  public static bool BlocksCurrency(ECurrencyType type, ECurrencySource source, int amount) =>
    SoulRule.ShouldBlock(type, source, amount, active: true);

  // ---- arena

  public static void OnSpawn(CCitadelPlayerController player, ITimer timer)
  {
    ArenaService.SendToArenaNextTick(player, Arena, timer);

    if (player.IsBot || !Heroes.IsPending(player.PlayerSteamId))
      return;

    var steamId = player.PlayerSteamId;

    timer.NextTick(() =>
    {
      if (Find(steamId) is { } current)
        Heroes.ApplyPending(current, timer);
    });
  }

  public static int ContainPlayers(ExecutionMode mode = ExecutionMode.Clean) =>
    ArenaService.ReturnStrays(Players.GetAll().Where(player => DeadlockTeams.IsPlayable(player.TeamNum)), Arena, mode);

  // ---- match

  public static void OnDeath(PlayerDeathEvent args, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!Session.IsPlaying)
      return;

    var attacker = args.AttackerController?.As<CCitadelPlayerController>();
    var victim = args.UseridController?.As<CCitadelPlayerController>();

    if (attacker == null || victim == null || attacker.IsBot)
      return;

    if (!GunGameRules.Credits(attacker.PlayerSteamId, victim.PlayerSteamId, attacker.TeamNum, victim.TeamNum))
      return;

    var kills = Session.Scores.Add(attacker.PlayerSteamId);
    Names[attacker.PlayerSteamId] = attacker.PlayerName;

    Log.WithMode(mode).Info(attacker.ToPlayerRef(), "Kill Kills={Kills} Victim={Victim} Match={Match}", kills, victim.PlayerName, Session.Match);
    Heroes.Roll(attacker, timer, mode);
  }

  public static string Reroll(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Debug)
  {
    var pick = Heroes.Roll(player, timer, mode);

    return pick == null
      ? $"{player.PlayerName}: no hero to draw."
      : $"{player.PlayerName} -> {Heroes.Catalog.DisplayName(pick.Hero)} ({pick.Build.Name}){(Heroes.IsPending(player.PlayerSteamId) ? ", on next spawn" : "")}.";
  }

  public static IReadOnlyList<string> DescribeFor(CCitadelPlayerController player)
  {
    if (!Session.IsPlaying)
      return [$"No match running ({Session.Phase}). {GunGameRules.WaitingLine(Humans().Count, Session.Options.MinPlayers)}"];

    var steamId = player.PlayerSteamId;
    List<string> lines =
    [
      $"Match {Session.Match} - {SessionRule.Clock(Session.SecondsLeft)} left",
      $"You: {GunGameRules.Kills(Session.Scores.PointsOf(steamId))}, place {Session.Scores.PlaceOf(steamId)}"
    ];

    foreach (var (id, points) in Session.Scores.Standings().Take(3))
      lines.Add($"{Session.Scores.PlaceOf(id)}  {NameOf(id)}  {points}");

    return lines;
  }

  public static IReadOnlyList<string> Describe() =>
  [
    .. Session.Describe(NameOf),
    $"Arena={Arena.Name} | ActiveLane={Arena.ActiveLane?.ToString() ?? "-"} | Pending={Heroes.PendingCount}",
    .. Heroes.Describe(NameOf)
  ];

  public static List<CCitadelPlayerController> Humans() =>
    Players.GetAll().Where(player => !player.IsBot).ToList();

  public static string NameOf(ulong steamId) =>
    Names.TryGetValue(steamId, out var name) ? name : Find(steamId)?.PlayerName ?? steamId.ToString();

  private static CCitadelPlayerController? Find(ulong steamId) =>
    Players.GetAll().FirstOrDefault(player => player.PlayerSteamId == steamId);

  private static RandomLoadouts CreateHeroes() =>
    new(HeroBuildCatalog.Default, new LoadoutOptions(Gold: 0), Random.Shared)
    {
      Landed = (player, pick, result) =>
      {
        var hero = HeroBuildCatalog.Default.DisplayName(pick.Hero);
        var kills = Session.Scores.PointsOf(player.PlayerSteamId);
        var (title, description) = Session.IsPlaying && kills > 0
          ? GunGameRules.KillBanner(kills, hero, pick.Build.Name, result.Value)
          : GunGameRules.HeroBanner(hero, pick.Build.Name, result.Value);

        HudService.Announce(player, title, description);
      }
    };

  private static TimedSession CreateSession() =>
    new(GunGameRules.Title, GunGameRules.Session, () => Humans().Count(player => DeadlockTeams.IsPlayable(player.TeamNum)))
    {
      Started = mode =>
      {
        var (title, description) = GunGameRules.StartBanner(Session.Options.MatchSeconds);
        HudService.AnnounceAll(title, description, mode);
      },
      Warned = (secondsLeft, mode) =>
      {
        var (title, description) = GunGameRules.WarningBanner(secondsLeft);
        HudService.AnnounceAll(title, description, mode);
      },
      Ended = (result, mode) =>
      {
        var (title, description) = GunGameRules.ResultBanner(result, NameOf, Session.Options.BreakSeconds);
        HudService.AnnounceAll(title, description, mode);

        foreach (var (steamId, points) in result.Standings)
          Log.WithMode(mode).Info("Final Match={Match} Player={Player} Steam={Steam} Kills={Kills}", result.Match, NameOf(steamId), steamId, points);
      },
      Stopped = mode =>
      {
        foreach (var player in Humans())
          PlayerChat.Send(player, GunGameRules.WaitingLine(Humans().Count, Session.Options.MinPlayers));
      }
    };
}
