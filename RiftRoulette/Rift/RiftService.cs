using Bublock.Shared;
using DeadworksManaged.Api;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.Rift;

public enum RiftPhase
{
  Idle,
  WaitingForSpawn,
  Live,
  Ending
}

public sealed record RiftRoundSteps(
  Action<RiftSide> MoveTeamsToRift,
  Func<int> ReturnPlayersToDraft,
  Action<RiftRoundResult> RoundEnded);

public sealed record RiftSnapshot(HashSet<int> Spawners, HashSet<int> Cashins);

public static class RiftService
{
  private const int SpawnTimeoutRuns = 320;
  private const string SpawnerName = "citadel_item_koth_spawner";
  private const string CashinName = "citadel_koth_cashin";
  private const string TrooperName = "npc_trooper";

  private static readonly int[] LateSweepSeconds = [5, 10];

  private static readonly Logger Log = BublockLog.For("Rift");

  private static HashSet<int>? _troopersBeforeRift;
  private static IHandle? _spawnWait;
  private static IHandle? _watch;
  private static IHandle? _endTimer;
  private static int _roundNumber;

  public static RiftSide NextSide { get; private set; } = RiftSide.Green;
  public static RiftPhase Phase { get; private set; } = RiftPhase.Idle;
  public static RiftSide? CurrentSide { get; private set; }
  public static string LastOutcome { get; private set; } = "none";

  public static bool IsRunning => Phase != RiftPhase.Idle;

  public static string RunRift(ITimer timer, RiftRoundSteps steps, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);

    if (IsRunning)
    {
      log.Info("Rift start refused, already running Phase={Phase}", Phase);
      return $"A rift is already running (Phase={Phase}). Use /rift_cancel.";
    }

    log.Info("Forcing Rift through GameRules");

    if (!TryResolveGameRules(log, out var gameRules))
      return "Could not reach CCitadelGameRules; see the rift log.";

    var side = NextSide;
    var position = RiftSides.Position(side);
    var snapshot = SnapshotRiftEntities();

    BeginRound(side);

    log.Info(
      "Spawning rift Side={Side} Position={Position} Existing={Existing} Cashins={Cashins}",
      RiftSides.Name(side),
      position,
      snapshot.Spawners.Count,
      snapshot.Cashins.Count);

    RiftGameRules.ConfigureNextRift(gameRules, position);

    _spawnWait = timer.Sequence(step => WaitForSpawner(step, timer, steps, gameRules, side, snapshot, mode));

    return $"Rift starting on {RiftSides.Name(side)}.";
  }

  public static RiftSnapshot SnapshotRiftEntities()
  {
    var snapshot = new RiftSnapshot(Snapshot(SpawnerName), Snapshot(CashinName));
    _troopersBeforeRift = Snapshot(TrooperName);
    return snapshot;
  }

  public static void AlternateSide(RiftSide spawnedSide)
  {
    NextSide = RiftSides.Other(spawnedSide);
  }

  public static bool SetNextSide(RiftSide side, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);

    if (IsRunning)
    {
      log.Info("Next side refused while running Phase={Phase}", Phase);
      return false;
    }

    NextSide = side;
    log.Info("Next rift set Side={Side}", RiftSides.Name(side));
    return true;
  }

  public static int CleanupRiftTroopers(ExecutionMode mode = ExecutionMode.Clean)
  {
    var removed = 0;

    foreach (var trooper in Entities.ByDesignerName(TrooperName))
    {
      trooper.Remove();
      removed++;
    }

    Log.WithMode(mode).Debug("Rift troopers removed Removed={Removed}", removed);
    return removed;
  }

  public static void ScheduleLateSweeps(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    foreach (var delay in LateSweepSeconds)
      timer.Once(delay.Seconds(), () => LateSweep(delay, mode));
  }

  public static void EndRound(
    string outcome,
    RiftRoundSteps steps,
    ITimer timer,
    ExecutionMode mode = ExecutionMode.Clean,
    int? winnerTeam = null)
  {
    var log = Log.WithMode(mode);

    log.Info("Ending rift round Outcome={Outcome}", outcome);

    var returned = steps.ReturnPlayersToDraft();
    var removed = CleanupRiftTroopers(mode);
    ScheduleLateSweeps(timer, mode);

    log.Info(
      "Rift round ended, players returned to draft Outcome={Outcome} Returned={Returned} TroopersRemoved={TroopersRemoved}",
      outcome,
      returned,
      removed);

    BublockLog.Master.Info("Rift round ended Outcome={Outcome} TroopersRemoved={TroopersRemoved}", outcome, removed);

    FinishRound(outcome, steps, winnerTeam);
  }

  public static string CancelRift(RiftRoundSteps steps, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);

    if (!IsRunning)
      return "No rift is running.";

    var phase = Phase;

    _spawnWait?.Cancel();
    _watch?.Cancel();
    _endTimer?.Cancel();

    if (TryResolveGameRules(log, out var gameRules))
      RiftGameRules.ParkScheduler(gameRules);
    else
      RiftGameRules.SetKothEnabled(false);

    var returned = steps.ReturnPlayersToDraft();
    var removed = CleanupRiftTroopers(mode);
    ScheduleLateSweeps(timer, mode);

    log.Info(
      "Rift cancelled Phase={Phase} Returned={Returned} TroopersRemoved={TroopersRemoved} NextRift={NextRift}",
      phase,
      returned,
      removed,
      RiftSides.Name(NextSide));

    BublockLog.Master.Info("Rift cancelled Phase={Phase}", phase);

    FinishRound(RiftOutcome.Cancelled, steps);

    return $"Rift cancelled during {phase}. Returned {returned} player(s), removed {removed} trooper(s). Next rift: {RiftSides.Name(NextSide)}.";
  }

  public static IReadOnlyList<string> DescribeRift() =>
  [
    $"Phase={Phase} | Current={(CurrentSide is { } side ? RiftSides.Name(side) : "-")} | Next={RiftSides.Name(NextSide)}",
    $"LastOutcome={LastOutcome} | Round={BublockLog.RoundId ?? "-"} | Snapshot={(_troopersBeforeRift == null ? "none" : $"{_troopersBeforeRift.Count} trooper(s)")}"
  ];

  private static Pace WaitForSpawner(
    IStep step,
    ITimer timer,
    RiftRoundSteps steps,
    nint gameRules,
    RiftSide side,
    RiftSnapshot snapshot,
    ExecutionMode mode)
  {
    var log = Log.WithMode(mode);
    var sideName = RiftSides.Name(side);

    var spawner = Entities
      .ByDesignerName(SpawnerName)
      .FirstOrDefault(entity => !snapshot.Spawners.Contains(entity.EntityIndex));

    if (spawner != null)
    {
      var spawnedSide = RiftSides.TryMatch(spawner.Position, out var matched) ? matched : side;

      log.Info(
        "Rift spawned Side={Side} Expected={Expected} Actual={Actual} Index={Index}",
        RiftSides.Name(spawnedSide),
        RiftSides.Position(side),
        spawner.Position,
        spawner.EntityIndex);

      BublockLog.Master.Info("Rift spawned Side={Side}", RiftSides.Name(spawnedSide));

      GoLive(timer, steps, gameRules, spawnedSide, mode);
      return step.Done();
    }

    // A cash-in that existed before the forced spawn is a leftover rift, and the
    // game will not spawn a new one until it gives up.
    var leftoverAtStart = snapshot.Cashins.Count > 0;

    if ((leftoverAtStart || step.Run >= SpawnTimeoutRuns) && FindRiftOnMap(side) is { } leftover)
    {
      log.Warn(
        "No new rift spawned, using the one on the map Side={Side} Requested={Requested} Entity={Entity} Index={Index} Position={Position} Runs={Runs}",
        RiftSides.Name(leftover.Side),
        sideName,
        leftover.DesignerName,
        leftover.Entity.EntityIndex,
        leftover.Entity.Position,
        step.Run);

      GoLive(timer, steps, gameRules, leftover.Side, mode);
      return step.Done();
    }

    if (step.Run >= SpawnTimeoutRuns)
    {
      RiftGameRules.ParkScheduler(gameRules);

      log.Warn("Rift spawn timed out Side={Side} NextRift={NextRift}", sideName, sideName);

      FinishRound(RiftOutcome.SpawnTimedOut, steps);
      return step.Done();
    }

    return step.Wait(1.Ticks());
  }

  private static void GoLive(ITimer timer, RiftRoundSteps steps, nint gameRules, RiftSide side, ExecutionMode mode)
  {
    var log = Log.WithMode(mode);
    var sideName = RiftSides.Name(side);

    RiftGameRules.ParkScheduler(gameRules);

    CurrentSide = side;
    steps.MoveTeamsToRift(side);
    log.Info("Players moved to rift starting positions Side={Side}", sideName);

    log.Debug("KOTH GiveUp={GiveUp}", RiftGameRules.KothGiveUp.Get(gameRules));

    AlternateSide(side);

    log.Info(
      "KOTH disabled NextRift={NextRift} NextWindow={NextWindow} NextSpawn={NextSpawn}",
      RiftSides.Name(NextSide),
      RiftGameRules.NextWindow.Get(gameRules),
      RiftGameRules.NextSpawn.Get(gameRules));

    Phase = RiftPhase.Live;

    var watch = new RiftWatch();
    _watch = timer.Sequence(watchStep => WatchOutcome(watchStep, timer, steps, gameRules, watch, mode));
  }

  private static (CBaseEntity Entity, RiftSide Side, string DesignerName)? FindRiftOnMap(RiftSide requested)
  {
    (CBaseEntity Entity, RiftSide Side, string DesignerName)? found = null;

    foreach (var spawner in Entities.ByDesignerName(SpawnerName))
    {
      if (!RiftSides.TryMatch(spawner.Position, out var side))
        continue;

      if (side == requested)
        return (spawner, side, SpawnerName);

      found ??= (spawner, side, SpawnerName);
    }

    if (found != null)
      return found;

    foreach (var cashin in Entities.ByDesignerName(CashinName))
    {
      var side = RiftSides.TryMatch(cashin.Position, out var matched)
        ? matched
        : RiftSides.Nearest(cashin.Position);

      if (side == requested)
        return (cashin, side, CashinName);

      found ??= (cashin, side, CashinName);
    }

    return found;
  }

  private static Pace WatchOutcome(
    IStep step,
    ITimer timer,
    RiftRoundSteps steps,
    nint gameRules,
    RiftWatch watch,
    ExecutionMode mode)
  {
    var log = Log.WithMode(mode);
    var troopers = _troopersBeforeRift ?? [];

    var newTrooper = Entities
      .ByDesignerName(TrooperName)
      .FirstOrDefault(entity => !troopers.Contains(entity.EntityIndex));

    var cashinExists = newTrooper == null
      && Entities.ByDesignerName(CashinName).Any();

    switch (watch.Observe(newTrooper != null, cashinExists))
    {
      case RiftObservation.CashinAppeared:
        var cashin = Entities.ByDesignerName(CashinName).FirstOrDefault();

        log.Debug(
          "KOTH cash-in appeared VData={VData} Handle={Handle} GiveUp={GiveUp}",
          cashin?.SubclassVData?.Name,
          cashin?.SubclassVData?.Handle,
          RiftGameRules.KothGiveUp.Get(gameRules));

        return step.Wait(1.Ticks());

      case RiftObservation.Finished:
        log.Info(
          "Rift finished, ending round in 3 seconds TrooperIndex={TrooperIndex} TrooperTeam={TrooperTeam}",
          newTrooper!.EntityIndex,
          newTrooper.TeamNum);
        ScheduleEnd(timer, steps, RiftOutcome.Finished, mode, newTrooper.TeamNum);
        return step.Done();

      case RiftObservation.Tied:
        log.Info("Rift tied, cash-in disappeared, ending round in 3 seconds");
        ScheduleEnd(timer, steps, RiftOutcome.Tied, mode);
        return step.Done();

      default:
        return step.Wait(1.Ticks());
    }
  }

  private static void ScheduleEnd(
    ITimer timer,
    RiftRoundSteps steps,
    string outcome,
    ExecutionMode mode,
    int? winnerTeam = null)
  {
    Phase = RiftPhase.Ending;
    _endTimer = timer.Once(3.Seconds(), () => EndRound(outcome, steps, timer, mode, winnerTeam));
  }

  private static void BeginRound(RiftSide side)
  {
    _roundNumber++;
    BublockLog.RoundId = $"r{_roundNumber}";

    Phase = RiftPhase.WaitingForSpawn;
    CurrentSide = side;

    BublockLog.Master.Info("Rift round started Side={Side}", RiftSides.Name(side));
  }

  private static void FinishRound(string outcome, RiftRoundSteps steps, int? winnerTeam = null)
  {
    var result = new RiftRoundResult(outcome, CurrentSide, winnerTeam);

    Phase = RiftPhase.Idle;
    CurrentSide = null;
    LastOutcome = outcome;

    _spawnWait = null;
    _watch = null;
    _endTimer = null;

    BublockLog.RoundId = null;

    try
    {
      steps.RoundEnded(result);
    }
    catch (Exception exception)
    {
      Log.Error(exception, "Round-ended step failed Outcome={Outcome}", outcome);
    }
  }

  private static bool TryResolveGameRules(Logger log, out nint gameRules)
  {
    switch (RiftGameRules.TryResolve(out gameRules))
    {
      case RiftGameRules.ResolveResult.ProxyMissing:
        log.Error("Could not find citadel_gamerules");
        return false;

      case RiftGameRules.ResolveResult.PointerNull:
        log.Error("CCitadelGameRules pointer was null");
        return false;

      default:
        return true;
    }
  }

  private static void LateSweep(int delay, ExecutionMode mode)
  {
    if (IsRunning)
    {
      Log.WithMode(mode).Debug("Late trooper sweep skipped, a rift is running Delay={Delay}", delay);
      return;
    }

    var removed = CleanupRiftTroopers(mode);

    if (removed > 0)
      Log.WithMode(mode).Info("Late rift troopers removed Removed={Removed} Delay={Delay}", removed, delay);
  }

  private static HashSet<int> Snapshot(string designerName) =>
    Entities
      .ByDesignerName(designerName)
      .Select(entity => entity.EntityIndex)
      .ToHashSet();
}
