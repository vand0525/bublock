using Bublock.Modules.Arena;
using Bublock.Modules.DevMode;
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

  // gg_bots: bots count as players for starting matches (solo testing). Off after every load.
  public static bool CountBots { get; private set; }

  // Dev: sandbox, no auto-start, environment commands allowed. Prod: the live session. Reset on every load.
  public static RunMode Mode { get; private set; } = GunGameRules.DefaultMode;

  public static PositionMemory DevPositions { get; } = new();

  // Dev-only commands call this; a refusal is thrown back to the caller.
  public static void RequireDev(string command)
  {
    if (DevRules.Refusal(Mode, command) is { } refusal)
      throw new CommandException(refusal);
  }

  // ---- dev / prod

  public static string Play(ITimer timer, ExecutionMode mode = ExecutionMode.Debug)
  {
    if (Session.IsPaused)
    {
      Session.Resume(timer, mode);
      return $"Resumed: {SessionRule.Clock(Session.SecondsLeft)} left.";
    }

    if (Mode == RunMode.Prod && Session.Phase != SessionPhase.Waiting)
      return $"A live session is already running (match {Session.Match}, {Session.Phase}).";

    var saved = Mode == RunMode.Dev ? DevPositions.Save(Humans()) : DevPositions.Count;
    SetMode(RunMode.Prod, mode);

    var (title, description) = GunGameRules.PlayBanner(Session.Options.MatchSeconds);
    HudService.AnnounceAll(title, description, mode);
    Session.Start(timer, mode);

    return $"Live session started (prod): match {Session.Match}, {SessionRule.Clock(Session.Options.MatchSeconds)}; {saved} dev position(s) saved for /stop.";
  }

  public static string Stop(ITimer timer, ExecutionMode mode = ExecutionMode.Debug)
  {
    if (Mode == RunMode.Dev && Session.Phase == SessionPhase.Waiting)
      return "Already in dev mode.";

    SetMode(RunMode.Dev, mode);
    Session.Stop(mode);

    var (title, description) = GunGameRules.StopBanner();
    HudService.AnnounceAll(title, description, mode);

    // Back to where everyone stood in dev, outside the stop command.
    timer.NextTick(() => DevPositions.Restore(Humans(), mode));
    return $"Dev mode: live session stopped; returning {DevPositions.Count} player(s) to their dev positions.";
  }

  public static IReadOnlyList<string> Pause(ITimer timer, ExecutionMode mode = ExecutionMode.Debug)
  {
    var paused = Session.Pause(mode);
    var lines = DebugSnapshot.Take(paused ? "pause" : $"pause ({DevRules.Name(Mode)}, {Session.Phase})", Describe(), mode);

    return paused
      ? [$"Paused with {SessionRule.Clock(Session.SecondsLeft)} left; /play resumes, /stop ends. Snapshot in debug log:", .. lines]
      : [$"Nothing running to pause ({DevRules.Name(Mode)}, {Session.Phase}); snapshot taken:", .. lines];
  }

  private static void SetMode(RunMode next, ExecutionMode mode)
  {
    Mode = next;
    Session.AutoStart = next == RunMode.Prod;
    Log.WithMode(mode).Info("Mode set Mode={Mode} AutoStart={AutoStart}", DevRules.Name(next), Session.AutoStart);
    BublockLog.Master.Info("Gun Game mode set Mode={Mode}", DevRules.Name(next));
  }

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

    if (Mode == RunMode.Prod && Session.Phase == SessionPhase.Waiting)
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

  // ---- testing (experimental server)

  public static string SetBots(int count, ITimer timer, ExecutionMode mode = ExecutionMode.Debug)
  {
    if (!GunGameRules.IsValidBotCount(count))
      return $"Bots must be 0 to {GunGameRules.MaxBots}.";

    if (count > 0)
      ServerConVars.TrySet("citadel_spawn_practice_bots_count", count, Log);

    ServerConVars.TrySet("citadel_spawn_practice_bots", count > 0 ? 1 : 0, Log);
    CountBots = count > 0;

    if (count == 0)
      Cheats.Run(() => Server.ExecuteCommand("bot_kick_all"));

    Log.WithMode(mode).Info("Practice bots set Count={Count} CountBots={CountBots}", count, CountBots);
    timer.Once(1.Seconds(), () => Session.Check(timer, mode));

    return count > 0
      ? $"Practice bots on ({count}); bots now count as players. Now: {Humans().Count} humans, {Bots().Count} bots. None yet? Try gg_map (reloads the map; you stay connected)."
      : $"Practice bots off, bots kicked. Now: {Humans().Count} humans, {Bots().Count} bots.";
  }

  public static string ReloadMap(ExecutionMode mode = ExecutionMode.Debug)
  {
    var map = Server.MapName;
    Log.WithMode(mode).Info("Map reload requested Map={Map}", map);
    BublockLog.Master.Info("Map reload requested Map={Map}", map);
    Server.ExecuteCommand($"changelevel {map}");
    return $"Reloading {map}; everyone stays connected.";
  }

  public static string Exec(CCitadelPlayerController? caller, string command, ExecutionMode mode = ExecutionMode.Debug)
  {
    if (string.IsNullOrWhiteSpace(command))
      return "Give a server command, e.g. gg_exec citadel_solo_bot_match 1";

    Log.WithMode(mode).Info("Server command from admin By={By} Command={Command}", caller?.PlayerName ?? "console", command);
    BublockLog.Master.Info("Server command from admin By={By} Command={Command}", caller?.PlayerName ?? "console", command);
    Server.ExecuteCommand(command);
    return $"Ran: {command} (output is in the server console / engine log).";
  }

  public static List<CCitadelPlayerController> Bots() =>
    Players.GetAll().Where(player => player.IsBot).ToList();

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

  // Containment is a live-session rule: in dev players may roam to test.
  public static int ContainPlayers(ExecutionMode mode = ExecutionMode.Clean) =>
    Mode != RunMode.Prod ? 0 : ArenaService.ReturnStrays(Players.GetAll().Where(player => DeadlockTeams.IsPlayable(player.TeamNum)), Arena, mode);

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
    $"Mode={DevRules.Name(Mode)}",
    .. Session.Describe(NameOf),
    $"Arena={Arena.Name} | ActiveLane={Arena.ActiveLane?.ToString() ?? "-"} | Pending={Heroes.PendingCount} | Humans={Humans().Count} | Bots={Bots().Count} | CountBots={CountBots}",
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
    new(GunGameRules.Title, GunGameRules.Session, () => Players.GetAll().Count(player =>
      (!player.IsBot || CountBots) && DeadlockTeams.IsPlayable(player.TeamNum)))
    {
      AutoStart = GunGameRules.DefaultMode == RunMode.Prod,
      PausedAt = (secondsLeft, mode) =>
      {
        var (title, description) = GunGameRules.PauseBanner(secondsLeft);
        HudService.AnnounceAll(title, description, mode);
      },
      Resumed = (secondsLeft, mode) => HudService.AnnounceAll("Resumed", $"{SessionRule.Clock(secondsLeft)} left", mode),
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
        if (Mode != RunMode.Prod)
          return;

        foreach (var player in Humans())
          PlayerChat.Send(player, GunGameRules.WaitingLine(Humans().Count, Session.Options.MinPlayers));
      }
    };
}
