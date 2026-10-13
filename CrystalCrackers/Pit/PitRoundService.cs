using System.Globalization;
using System.Numerics;
using Bublock.Modules.Hud;
using Bublock.Modules.Loadout;
using Bublock.Modules.MidBoss;
using Bublock.Modules.Movement;
using Bublock.Modules.RandomHeroes;
using Bublock.Modules.Restraint;
using Bublock.Shared;
using DeadworksManaged.Api;
using ITimer = DeadworksManaged.Api.ITimer;

namespace CrystalCrackers.Pit;

public sealed record PitAssignment(Heroes Hero, HeroBuild Build, int Team);

public static class PitRoundService
{
  public const double RecheckSeconds = 1.0;

  public static readonly LoadoutOptions Options = new(Gold: 0);

  private static readonly Logger Log = BublockLog.For("Pit");

  private static readonly Dictionary<ulong, PitAssignment> Fighters = [];
  private static readonly Dictionary<ulong, Heroes> LastHero = [];
  private static readonly HashSet<ulong> Pending = [];
  private static readonly HashSet<ulong> Out = [];
  private static readonly HashSet<ulong> CreditSeen = [];
  private static readonly Dictionary<ulong, (DateTime At, bool Reborn)> Deaths = [];
  private static readonly Dictionary<int, int> Wins = [];

  private static IHandle? _countdown;
  private static IHandle? _limit;
  private static IHandle? _window;
  private static IHandle? _recheck;

  private static ITimer? _timer;
  private static ExecutionMode _mode = ExecutionMode.Clean;
  private static DateTime _phaseStarted;
  private static int _killerTeam;
  private static int _crystalTeam;

  public static PitPhase Phase { get; private set; } = PitPhase.Idle;
  public static int Round { get; private set; }
  public static int PitTeam { get; private set; }
  public static int LastPitTeam { get; private set; }
  public static int WindowSeconds { get; private set; } = PitRoundRule.DefaultWindowSeconds;
  public static int LimitSeconds { get; private set; } = PitRoundRule.DefaultLimitSeconds;

  public static bool IsFighter(ulong steamId) => Phase != PitPhase.Idle && Fighters.ContainsKey(steamId);

  // Prep: heroes and builds at the soul cap, everyone restrained up top, then the countdown.
  public static string Start(int chosenPitTeam, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (Phase != PitPhase.Idle)
      throw new CommandException($"Round {Round} is already running ({Phase}). Use /pit_end first.");

    var players = TeamPlayers();

    if (players.Count == 0)
      throw new CommandException("Nobody is on Amber or Sapphire.");

    var log = Log.WithMode(mode);
    var catalog = HeroBuildCatalog.Default;

    ClearRound();
    _timer = timer;
    _mode = mode;
    Round++;
    PitTeam = PitRoundRule.PitTeam(chosenPitTeam, LastPitTeam);
    SetPhase(PitPhase.Countdown);

    var heroes = HeroDraw.Draw(players.Select(player => player.PlayerSteamId).ToList(), catalog.Heroes, LastHero, Random.Shared);

    foreach (var player in players)
    {
      var steamId = player.PlayerSteamId;

      if (!heroes.TryGetValue(steamId, out var hero))
        continue;

      var builds = catalog.BuildsFor(hero);
      Fighters[steamId] = new PitAssignment(hero, builds[Random.Shared.Next(builds.Count)], player.TeamNum);
      LastHero[steamId] = hero;
    }

    var swapped = 0;

    foreach (var player in players)
    {
      SendUp(player, mode);

      if (!Fighters.ContainsKey(player.PlayerSteamId))
        continue;

      if (StartSwap(player, mode))
        swapped++;
      else
        Pending.Add(player.PlayerSteamId);
    }

    HudService.AnnounceAll($"Round {Round}", $"{PitRoundRule.TeamName(PitTeam)} starts in the pit", mode);

    var line = "Pit round prepared Round={Round} PitTeam={PitTeam} Fighters={Fighters} Sapphire={Sapphire} Amber={Amber} Swapped={Swapped} Pending={Pending} Cap={Cap}";
    object?[] values =
    [
      Round, PitRoundRule.TeamName(PitTeam), Fighters.Count, TeamCount(PitRoundRule.SapphireTeam),
      TeamCount(PitRoundRule.AmberTeam), swapped, Pending.Count, LoadoutService.MaxValue
    ];
    log.Info(line, values);
    BublockLog.Master.Info(line, values);

    _countdown = timer.Once(PitRoundRule.CountdownSeconds.Seconds(), () => GoLive(timer, mode));

    return $"Round {Round}: {Fighters.Count} fighters, {PitRoundRule.TeamName(PitTeam)} in the pit, " +
           $"{swapped} heroes swapped ({Pending.Count} wait for a respawn), live in {PitRoundRule.CountdownSeconds}s.";
  }

  private static void GoLive(ITimer timer, ExecutionMode mode)
  {
    if (Phase != PitPhase.Countdown)
      return;

    var log = Log.WithMode(mode);
    var ready = MidBossService.Ready(LoadoutService.MaxValue, Math.Max(1, Fighters.Count), mode);
    _recheck = timer.Once(RecheckSeconds.Seconds(), () => MidBossService.Recheck(ready, mode));

    SetPhase(PitPhase.Live);
    var placed = 0;

    foreach (var player in Players.GetAll())
    {
      if (!Fighters.ContainsKey(player.PlayerSteamId) || Pending.Contains(player.PlayerSteamId))
        continue;

      if (PlaceInRound(player, mode))
        placed++;
    }

    foreach (var player in Players.GetAll())
    {
      if (!Fighters.ContainsKey(player.PlayerSteamId))
        continue;

      HudService.Announce(
        player,
        PitRoundRule.StartsInPit(Fighters[player.PlayerSteamId].Team, PitTeam) ? "Kill the mid-boss" : "Steal the crystal",
        PitRoundRule.StartsInPit(Fighters[player.PlayerSteamId].Team, PitTeam)
          ? "Hold the pit and take its crystal"
          : "Get into the pit and take the crystal",
        mode);
    }

    _limit = timer.Once(LimitSeconds.Seconds(), () => End(PitEndReason.TimeLimit, mode));

    var line = "Pit round live Round={Round} PitTeam={PitTeam} Placed={Placed} Pending={Pending} BossHealth={BossHealth} Scale={Scale} LimitSeconds={LimitSeconds}";
    object?[] values = [Round, PitRoundRule.TeamName(PitTeam), placed, Pending.Count, ready.Health, ready.Scale, LimitSeconds];
    log.Info(line, values);
    BublockLog.Master.Info(line, values);
  }

  public static void OnBossKilled(int team)
  {
    if (Phase != PitPhase.Live || _timer is not { } timer)
      return;

    _killerTeam = team;
    _limit?.Cancel();
    SetPhase(PitPhase.BossDown);

    HudService.AnnounceAll($"{PitRoundRule.TeamName(team)} killed the boss", "Grab the crystal", _mode);
    _window = timer.Once(WindowSeconds.Seconds(), () => End(PitEndReason.WindowOver, _mode));

    BublockLog.Master.Info(
      "Pit boss down Round={Round} KillerTeam={KillerTeam} WindowSeconds={WindowSeconds}",
      Round, PitRoundRule.TeamName(team), WindowSeconds);
  }

  // From the game's rejuv counts; the rebirth credit scan (WatchCredit) covers counts that no longer rise.
  public static void OnCrystalTaken(int team) => TakeCrystal(team, "count");

  // Called every second: who holds the crystal's rebirth credit, so a death with it is a rebirth.
  public static void WatchCredit()
  {
    if (!PitRoundRule.IsFighting(Phase))
      return;

    foreach (var player in Players.GetAll())
    {
      var pawn = player.GetHeroPawn();

      if (pawn == null || !pawn.IsAlive || !Fighters.ContainsKey(player.PlayerSteamId) || !MidBossService.HasRebirth(pawn))
        continue;

      if (CreditSeen.Add(player.PlayerSteamId))
        Log.WithMode(_mode).Info(player.ToPlayerRef(), "Rebirth credit held Team={Team}", PitRoundRule.TeamName(player.TeamNum));

      if (Phase == PitPhase.BossDown)
        TakeCrystal(player.TeamNum, "credit");
    }
  }

  private static void TakeCrystal(int team, string source)
  {
    if (Phase != PitPhase.BossDown || _crystalTeam != 0 || !PitRoundRule.IsTeam(team))
      return;

    _crystalTeam = team;
    HudService.AnnounceAll($"{PitRoundRule.TeamName(team)} took the crystal", "", _mode);
    BublockLog.Master.Info("Pit crystal taken Round={Round} Team={Team} Source={Source}", Round, PitRoundRule.TeamName(team), source);
  }

  public static void OnDeath(CCitadelPlayerController player, CBaseEntity? pawn)
  {
    var steamId = player.PlayerSteamId;

    if (!PitRoundRule.IsFighting(Phase) || !Fighters.ContainsKey(steamId) || Out.Contains(steamId))
      return;

    var credit = pawn != null && MidBossService.HasRebirth(pawn);
    var seen = CreditSeen.Remove(steamId);
    Deaths[steamId] = (DateTime.UtcNow, credit || seen);

    Log.WithMode(_mode).Info(
      player.ToPlayerRef(),
      "Fighter died CreditAtDeath={CreditAtDeath} CreditSeen={CreditSeen} Position={Position}",
      credit, seen, pawn == null ? "-" : Format(pawn.Position));
  }

  public static void OnSpawn(CCitadelPlayerController player, ITimer timer)
  {
    if (player.IsBot)
      return;

    var steamId = player.PlayerSteamId;
    var isFighter = Fighters.ContainsKey(steamId);
    var died = Deaths.Remove(steamId, out var death);
    var action = PitRoundRule.OnRespawn(Phase, isFighter, died, died && death.Reborn, Round > 0);

    if (isFighter && Pending.Contains(steamId))
      timer.NextTick(() => ApplyPending(steamId));

    if (action == RespawnAction.None)
      return;

    timer.NextTick(() =>
    {
      if (Find(steamId) is not { } current)
        return;

      if (action == RespawnAction.KeepIn)
        KeepIn(current, (DateTime.UtcNow - death.At).TotalSeconds);
      else
        SendOut(current, died);
    });
  }

  private static void KeepIn(CCitadelPlayerController player, double secondsDead)
  {
    var pawn = player.GetHeroPawn();
    var position = pawn?.Position ?? Vector3.Zero;
    var far = pawn != null && PitRoundRule.IsFarFromPit(position);

    if (far)
      MovementService.TeleportTo(player, StartSpot(player) ?? PitSpots.Watch(player.Slot), _mode);

    Log.WithMode(_mode).Info(
      player.ToPlayerRef(),
      "Reborn fighter kept in SecondsDead={SecondsDead} Position={Position} MovedBack={MovedBack}",
      Math.Round(secondsDead, 1), Format(position), far);
  }

  private static void SendOut(CCitadelPlayerController player, bool died)
  {
    SendUp(player, _mode);

    if (!died || !PitRoundRule.IsFighting(Phase) || !Out.Add(player.PlayerSteamId))
      return;

    HudService.Announce(player, "You're out", "Back next round", _mode);
    Log.WithMode(_mode).Info(player.ToPlayerRef(), "Fighter out for the round Team={Team} OutOnTeam={OutOnTeam}",
      PitRoundRule.TeamName(player.TeamNum), Out.Count(id => Fighters.TryGetValue(id, out var a) && a.Team == player.TeamNum));
  }

  // Ends the round: winner and score, boss and crystals removed, rebirth cleared, everyone up top.
  public static string End(PitEndReason reason, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (Phase == PitPhase.Idle)
      throw new CommandException("No pit round is running.");

    CancelTimers();

    var winner = PitRoundRule.Winner(reason, _crystalTeam, _killerTeam);

    if (winner != 0)
      Wins[winner] = Wins.GetValueOrDefault(winner) + 1;

    var bosses = MidBossService.RemoveBosses();
    var crystals = MidBossService.RemoveCrystals();
    var cleared = 0;
    var sent = 0;

    foreach (var player in Players.GetAll())
    {
      var pawn = player.GetHeroPawn();

      if (pawn != null)
        cleared += MidBossService.ClearRejuv(pawn);

      if (player.IsBot || !PitRoundRule.IsTeam(player.TeamNum))
        continue;

      if (pawn is { IsAlive: true })
      {
        SendUp(player, mode);
        sent++;
      }
      else
      {
        RestraintService.Restrain(player, mode);
      }
    }

    var score = PitRoundRule.Score(Wins);
    var title = winner != 0 ? $"{PitRoundRule.TeamName(winner)} wins round {Round}" : $"Round {Round}: no winner";
    var how = winner == 0 ? PitRoundRule.EndLine(reason)
      : winner == _crystalTeam ? "Took the crystal"
      : "Killed the boss";
    HudService.AnnounceAll(title, $"{how} | {score}", mode);

    LastPitTeam = PitTeam;
    SetPhase(PitPhase.Idle);
    Pending.Clear();
    CreditSeen.Clear();
    Deaths.Clear();

    var line = "Pit round ended Round={Round} Reason={Reason} Winner={Winner} KillerTeam={KillerTeam} CrystalTeam={CrystalTeam} Out={Out} BossesRemoved={BossesRemoved} CrystalsRemoved={CrystalsRemoved} RejuvCleared={RejuvCleared} SentUp={SentUp} Score={Score}";
    object?[] values =
    [
      Round, reason, PitRoundRule.TeamName(winner), PitRoundRule.TeamName(_killerTeam), PitRoundRule.TeamName(_crystalTeam),
      Out.Count, bosses, crystals, cleared, sent, score
    ];
    Log.WithMode(mode).Info(line, values);
    BublockLog.Master.Info(line, values);

    return $"{title}. {how}. {score}.";
  }

  public static string SetTiming(int? window, int? limit, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (window is { } w)
      WindowSeconds = w;

    if (limit is { } l)
      LimitSeconds = l;

    if (window != null || limit != null)
      Log.WithMode(mode).Info("Pit timing set WindowSeconds={WindowSeconds} LimitSeconds={LimitSeconds}", WindowSeconds, LimitSeconds);

    return $"Crystal window {WindowSeconds}s, time limit {LimitSeconds}s (from the next timer started).";
  }

  // A map start drops the round (plugin statics survive a map change, pawns and the boss do not); the score stays.
  public static void ResetForMap()
  {
    if (Phase != PitPhase.Idle)
      BublockLog.Master.Info("Pit round dropped by a map start Round={Round} Phase={Phase}", Round, Phase);

    CancelTimers();
    ClearRound();
    SetPhase(PitPhase.Idle);
  }

  public static void Forget(ulong steamId)
  {
    Pending.Remove(steamId);
    CreditSeen.Remove(steamId);
    Deaths.Remove(steamId);
  }

  public static IReadOnlyList<string> Describe()
  {
    var catalog = HeroBuildCatalog.Default;
    var lines = new List<string>
    {
      $"Phase {Phase}, round {Round}, pit team {PitRoundRule.TeamName(PitTeam)} (last {PitRoundRule.TeamName(LastPitTeam)}), " +
      $"score {PitRoundRule.Score(Wins)}{TimeLeft()}",
      $"Window {WindowSeconds}s, limit {LimitSeconds}s, cap {LoadoutService.MaxValue:N0} souls, " +
      $"killer {PitRoundRule.TeamName(_killerTeam)}, crystal {PitRoundRule.TeamName(_crystalTeam)}"
    };

    foreach (var team in new[] { PitRoundRule.SapphireTeam, PitRoundRule.AmberTeam })
    {
      var ids = Fighters.Where(pair => pair.Value.Team == team).Select(pair => pair.Key).ToList();
      lines.Add($"{PitRoundRule.TeamName(team)}: {ids.Count - ids.Count(Out.Contains)} in, {ids.Count(Out.Contains)} out");
    }

    foreach (var player in Players.GetAll().Where(player => !player.IsBot))
    {
      var steamId = player.PlayerSteamId;
      var hero = Fighters.TryGetValue(steamId, out var assignment)
        ? $"{catalog.DisplayName(assignment.Hero)} ({assignment.Build.Name})"
        : "-";
      var state = !Fighters.ContainsKey(steamId) ? "not in the round"
        : Out.Contains(steamId) ? "OUT"
        : Pending.Contains(steamId) ? "waiting for respawn"
        : "in";
      var credit = CreditSeen.Contains(steamId) ? " | rebirth credit" : "";

      lines.Add($"Slot={player.Slot} | {player.PlayerName} | {PitRoundRule.TeamName(player.TeamNum)} | {hero} | {state}{credit}");
    }

    lines.AddRange(MidBossService.Describe().Take(2));
    return lines;
  }

  private static string TimeLeft()
  {
    var total = Phase switch
    {
      PitPhase.Countdown => PitRoundRule.CountdownSeconds,
      PitPhase.Live => LimitSeconds,
      PitPhase.BossDown => WindowSeconds,
      _ => 0
    };

    if (total == 0)
      return "";

    var left = Math.Max(0, total - (DateTime.UtcNow - _phaseStarted).TotalSeconds);
    return $", {left.ToString("0", CultureInfo.InvariantCulture)}s left in {Phase}";
  }

  private static bool StartSwap(CCitadelPlayerController player, ExecutionMode mode)
  {
    var steamId = player.PlayerSteamId;

    if (!Fighters.TryGetValue(steamId, out var assignment) || _timer is not { } timer)
      return false;

    return LoadoutService.Swap(
      player,
      assignment.Hero,
      assignment.Build,
      timer,
      Options,
      mode,
      (current, result) =>
      {
        if (!Fighters.TryGetValue(steamId, out var live) || live != assignment)
          return;

        HudService.Announce(
          current,
          HeroBuildCatalog.Default.DisplayName(assignment.Hero),
          $"{assignment.Build.Name} - {result.Value.ToString("N0", CultureInfo.InvariantCulture)} souls",
          mode);
        Place(current, mode);
      },
      current => Log.WithMode(mode).Warn(current.ToPlayerRef(), "Hero swap failed Hero={Hero}", assignment.Hero));
  }

  private static void ApplyPending(ulong steamId)
  {
    if (Phase == PitPhase.Idle || !Pending.Contains(steamId) || Find(steamId) is not { } player)
      return;

    if (!StartSwap(player, _mode))
      return;

    Pending.Remove(steamId);
    Log.WithMode(_mode).Info(player.ToPlayerRef(), "Pending hero swap started after spawn Hero={Hero}", Fighters[steamId].Hero);
  }

  // After a swap lands: in the fight while the round is live (unless out), otherwise up top.
  private static void Place(CCitadelPlayerController player, ExecutionMode mode)
  {
    if (PitRoundRule.IsFighting(Phase) && !Out.Contains(player.PlayerSteamId))
      PlaceInRound(player, mode);
    else
      SendUp(player, mode);
  }

  private static bool PlaceInRound(CCitadelPlayerController player, ExecutionMode mode)
  {
    var pawn = player.GetHeroPawn();

    if (pawn == null || !pawn.IsAlive || StartSpot(player) is not { } spot)
      return false;

    RestraintService.Release(player, mode);
    MidBossService.ClearRejuv(pawn);
    MovementService.TeleportTo(player, spot, mode);
    pawn.Heal(pawn.GetMaxHealth());
    pawn.ResetAllAbilityCooldowns();

    Log.WithMode(mode).Info(player.ToPlayerRef(), "Fighter placed Spot={Spot}", spot.Name);
    return true;
  }

  private static MovementLocation? StartSpot(CCitadelPlayerController player)
  {
    if (!Fighters.TryGetValue(player.PlayerSteamId, out var assignment))
      return null;

    return PitRoundRule.StartsInPit(assignment.Team, PitTeam)
      ? PitSpots.Pit(assignment.Team, player.Slot)
      : PitSpots.Lane(assignment.Team, player.Slot);
  }

  private static void SendUp(CCitadelPlayerController player, ExecutionMode mode)
  {
    RestraintService.Restrain(player, mode);
    MovementService.TeleportTo(player, PitSpots.Watch(player.Slot), mode);
  }

  private static void SetPhase(PitPhase phase)
  {
    Phase = phase;
    _phaseStarted = DateTime.UtcNow;
  }

  private static void ClearRound()
  {
    Fighters.Clear();
    Pending.Clear();
    Out.Clear();
    CreditSeen.Clear();
    Deaths.Clear();
    _killerTeam = 0;
    _crystalTeam = 0;
  }

  private static void CancelTimers()
  {
    _countdown?.Cancel();
    _limit?.Cancel();
    _window?.Cancel();
    _recheck?.Cancel();
    _countdown = _limit = _window = _recheck = null;
  }

  private static List<CCitadelPlayerController> TeamPlayers() =>
    Players.GetAll().Where(player => !player.IsBot && PitRoundRule.IsTeam(player.TeamNum)).ToList();

  private static int TeamCount(int team) => Fighters.Values.Count(assignment => assignment.Team == team);

  private static CCitadelPlayerController? Find(ulong steamId) =>
    Players.GetAll().FirstOrDefault(player => player.PlayerSteamId == steamId);

  private static string Format(Vector3 position) =>
    $"({position.X:0}, {position.Y:0}, {position.Z:0})";
}
