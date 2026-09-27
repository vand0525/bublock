using RiftRoulette.Balance;
using RiftRoulette.Lobby;

namespace Bublock.Tests.RiftRoulette;

public class BalanceTrackerTests
{
  private const int S = RiftRouletteTeams.Sapphire;
  private const int A = RiftRouletteTeams.Amber;

  private static void Kills(BalanceTracker tracker, int team, int count)
  {
    for (var i = 0; i < count; i++)
      tracker.RecordKill(team);
  }

  [Fact]
  public void Stomp_needs_round_lead_and_big_kill_gap()
  {
    var tracker = new BalanceTracker();

    tracker.RecordRound(S);
    tracker.RecordRound(S);
    Kills(tracker, S, 10);
    Kills(tracker, A, 2);

    Assert.Equal(new BalanceVerdict(S, BalanceReason.Stomp), tracker.Check());
  }

  [Fact]
  public void Round_lead_with_close_kills_is_not_a_stomp()
  {
    var tracker = new BalanceTracker();

    tracker.RecordRound(A);
    tracker.RecordRound(A);
    tracker.RecordRound(A);
    Kills(tracker, A, 20);
    Kills(tracker, S, 14);

    Assert.Null(tracker.Check());
  }

  [Fact]
  public void Kill_gap_without_round_lead_is_not_a_stomp()
  {
    var tracker = new BalanceTracker();

    tracker.RecordRound(S);
    Kills(tracker, S, 20);

    Assert.Null(tracker.Check());
  }

  [Fact]
  public void Five_round_streak_triggers_and_other_outcomes_do_not_break_it()
  {
    var tracker = new BalanceTracker();

    for (var i = 0; i < 4; i++)
    {
      tracker.RecordRound(A);
      tracker.RecordRound(null);
    }

    Assert.Null(tracker.Check());

    tracker.RecordRound(A);

    Assert.Equal(new BalanceVerdict(A, BalanceReason.Streak), tracker.Check());
  }

  [Fact]
  public void Win_by_the_other_team_restarts_the_streak()
  {
    var tracker = new BalanceTracker();

    for (var i = 0; i < 4; i++)
      tracker.RecordRound(A);

    tracker.RecordRound(S);

    Assert.Equal(S, tracker.StreakTeam);
    Assert.Equal(1, tracker.Streak);
  }

  [Fact]
  public void Reset_clears_everything()
  {
    var tracker = new BalanceTracker();

    for (var i = 0; i < 5; i++)
      tracker.RecordRound(S);

    Kills(tracker, S, 12);
    tracker.Reset();

    Assert.Null(tracker.Check());
    Assert.Equal(0, tracker.SapphireRounds + tracker.SapphireKills + tracker.Streak);
  }

  [Fact]
  public void Leader_uses_rounds_then_kills()
  {
    var tracker = new BalanceTracker();

    Assert.Equal(S, tracker.Leader());

    Kills(tracker, A, 1);
    Assert.Equal(A, tracker.Leader());

    tracker.RecordRound(S);
    Assert.Equal(S, tracker.Leader());
  }
}
