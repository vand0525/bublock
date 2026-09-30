using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.GameLoop;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.Lobby;

public static class MapRefreshService
{
  public const int WarningSeconds = 10;

  public const int FallbackSeconds = 60;

  public const string Reason = "join-budget";

  public const string WarningLine =
    "Server refresh in 10s: the map reloads to keep joins working. " +
    "You'll reconnect automatically, so don't leave. A new match starts right after.";

  private static readonly Logger Log = BublockLog.For("Restart");

  public static int FighterRounds { get; private set; }

  public static int Budget { get; private set; } = MapRefreshRule.DefaultBudget;

  public static int LastFighters { get; private set; }

  public static bool Pending { get; private set; }

  public static void AddRound(int fighters, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (fighters <= 0)
      return;

    FighterRounds += fighters;
    LastFighters = fighters;
    Log.WithMode(mode).Info(
      "Round counted Fighters={Fighters} FighterRounds={FighterRounds} Budget={Budget}",
      fighters, FighterRounds, Budget);
  }

  public static void OnMapStart()
  {
    Log.Info("Map started, join budget reset FighterRounds={FighterRounds} Pending={Pending}", FighterRounds, Pending);
    FighterRounds = 0;
    LastFighters = 0;
    Pending = false;
  }

  // Called at a scored round end. True means the match was ended and a reload is on its way.
  public static bool TryBegin(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (Pending || !MapRefreshRule.Due(FighterRounds, Budget))
      return false;

    var log = Log.WithMode(mode);
    Pending = true;

    var told = 0;

    foreach (var player in Players.GetAll())
    {
      PlayerChat.Send(player, WarningLine);
      told++;
    }

    log.Warn(
      "Join budget reached, ending the match and reloading in {Seconds}s FighterRounds={FighterRounds} Budget={Budget} Told={Told}",
      WarningSeconds, FighterRounds, Budget, told);

    var ended = MatchService.End(timer, mode);
    log.Info("Match ended for the refresh Reply={Reply}", ended);

    timer.Once(WarningSeconds.Seconds(), () => Reload(timer, mode));
    return true;
  }

  public static string SetBudget(int budget, ExecutionMode mode = ExecutionMode.Clean)
  {
    Budget = Math.Max(0, budget);
    Log.WithMode(mode).Info("Join budget set Budget={Budget} FighterRounds={FighterRounds}", Budget, FighterRounds);
    BublockLog.Master.Info("Join budget set Budget={Budget}", Budget);

    return Budget == 0
      ? "Join budget refresh off until the next upload."
      : $"Join budget set to {Budget} fighter-rounds until the next upload ({FighterRounds} played since map start).";
  }

  public static string Describe()
  {
    if (Budget == 0)
      return $"Join budget refresh off. Fighter-rounds since map start: {FighterRounds}.";

    var left = MapRefreshRule.RoundsLeft(FighterRounds, Budget, LastFighters);
    var rounds = left is { } count ? $" (~{count} rounds left at {LastFighters / 2}v{LastFighters / 2})" : "";
    var pending = Pending ? " Reload pending." : "";
    return $"Join budget: {FighterRounds}/{Budget} fighter-rounds since map start{rounds}; reloads at a round end, then a fresh match.{pending}";
  }

  private static void Reload(ITimer timer, ExecutionMode mode)
  {
    if (!Pending)
      return;

    var reply = AutoRestartService.Restart(Reason, mode);
    Log.WithMode(mode).Info("Refresh reload asked Reply={Reply}", reply);

    timer.Once(FallbackSeconds.Seconds(), () => GiveUp(timer, mode));
  }

  // OnMapStart clears Pending; still set means the map never changed.
  private static void GiveUp(ITimer timer, ExecutionMode mode)
  {
    if (!Pending)
      return;

    Pending = false;
    Log.WithMode(mode).Warn(
      "Refresh reload did not happen, letting auto-start run again FighterRounds={FighterRounds}",
      FighterRounds);
    AutoStartService.Check(timer, mode);
  }
}
