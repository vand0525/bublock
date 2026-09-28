using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Lobby;
using RiftRoulette.Stats;

namespace RiftRoulette.Balance;

public static class BalanceService
{
  private static readonly Logger Log = BublockLog.For("Balance");

  public static BalanceTracker Tracker { get; } = new();

  public static bool Enabled { get; private set; } = true;

  public static void SetEnabled(bool enabled, ExecutionMode mode = ExecutionMode.Clean)
  {
    Enabled = enabled;
    Log.WithMode(mode).Info("Auto-balance set Enabled={Enabled}", enabled);
  }

  public static void Reset(ExecutionMode mode = ExecutionMode.Clean)
  {
    Tracker.Reset();
    Log.WithMode(mode).Debug("Balance counters reset");
  }

  public static void RecordRound(int? pointTo, ExecutionMode mode = ExecutionMode.Clean)
  {
    Tracker.RecordRound(pointTo);
    Log.WithMode(mode).Debug("Round recorded {Tracker}", Tracker.Describe());
  }

  public static void RecordKill(int team) => Tracker.RecordKill(team);

  public static string? TryBalance(Dictionary<ulong, int> teams, ExecutionMode mode = ExecutionMode.Clean, bool force = false)
  {
    var log = Log.WithMode(mode);

    // A 1v1's rounds and kills must not carry into a bigger match and swap people there.
    if (teams.Count < BalancePicker.MinPlayers)
    {
      Tracker.Reset();
      log.Debug("Balance off, fewer than 3 fighting Fighters={Fighters}", teams.Count);
      return null;
    }

    if (!Enabled && !force)
      return null;

    var verdict = Tracker.Check() ?? (force ? new BalanceVerdict(Tracker.Leader(), BalanceReason.Forced) : null);

    if (verdict == null)
    {
      log.Debug("Balance check, teams fine {Tracker}", Tracker.Describe());
      return null;
    }

    var candidates = teams
      .Select(pair => new BalanceCandidate(pair.Key, pair.Value, StatsService.Ledger.Get(pair.Key).Score))
      .ToList();

    var move = BalancePicker.Pick(candidates, verdict.Team);

    if (move == null)
    {
      log.Info(
        "Balance needed but no move possible Reason={Reason} Team={Team} Players={Players}",
        verdict.Reason,
        RiftRouletteTeams.Name(verdict.Team),
        candidates.Count);
      return null;
    }

    var losingTeam = RiftRouletteTeams.Other(verdict.Team);
    teams[move.Best] = losingTeam;

    if (move.Weakest is { } weakest)
      teams[weakest] = verdict.Team;

    var bestName = NameOf(move.Best);
    var text = move.Weakest is { } swapped
      ? $"Auto-balance: {bestName} <-> {NameOf(swapped)}"
      : $"Auto-balance: {bestName} moves to {RiftRouletteTeams.Name(losingTeam)}";

    log.Info("{Text} Reason={Reason} {Tracker}", text, verdict.Reason, Tracker.Describe());
    BublockLog.Master.Info("{Text} Reason={Reason}", text, verdict.Reason);

    foreach (var player in Players.GetAll().Where(player => !player.IsBot))
      PlayerChat.Send(player, text);

    Tracker.Reset();
    return text;
  }

  public static IReadOnlyList<string> Describe()
  {
    var verdict = Tracker.Check();

    return
    [
      $"Enabled={Enabled} | Verdict={(verdict == null ? "none" : $"{verdict.Reason} {RiftRouletteTeams.Name(verdict.Team)}")}",
      Tracker.Describe(),
      $"Rules: stomp = lead {BalanceTracker.LeadRounds}+ rounds and {BalanceTracker.StompKillDiff}+ more kills at {BalanceTracker.StompKillRatio}x; " +
      $"streak = {BalanceTracker.StreakRounds} rounds in a row",
      $"Needs {BalancePicker.MinPlayers}+ fighting (the bench does not count); counters reset while fewer fight"
    ];
  }

  private static string NameOf(ulong steamId) =>
    Players.GetAll().FirstOrDefault(player => player.PlayerSteamId == steamId)?.PlayerName ?? steamId.ToString();
}
