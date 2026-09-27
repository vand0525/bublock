using RiftRoulette.GameLoop;
using RiftRoulette.Lobby;
using RiftRoulette.Rift;

namespace Bublock.Tests.RiftRoulette;

public class MatchStateTests
{
  private static RiftRoundResult Finished(int? team) => new(RiftOutcome.Finished, RiftSide.Green, team);

  [Fact]
  public void New_state_is_idle_with_zero_score()
  {
    var state = new MatchState();

    Assert.Equal(MatchPhase.Idle, state.Phase);
    Assert.False(state.IsRunning);
    Assert.Equal("Sapphire 0 - 0 Amber", state.FormatScore());
  }

  [Fact]
  public void Start_enters_intermission_and_begin_round_counts_rounds()
  {
    var state = new MatchState();

    state.Start();
    Assert.Equal(MatchPhase.Intermission, state.Phase);

    state.BeginRound();
    state.EnterIntermission();
    state.BeginRound();

    Assert.Equal(MatchPhase.InRound, state.Phase);
    Assert.Equal(2, state.Round);
  }

  [Fact]
  public void Finished_scores_for_the_winner_team()
  {
    var state = new MatchState();

    Assert.Equal(RiftRouletteTeams.Sapphire, state.Apply(Finished(RiftRouletteTeams.Sapphire)));
    Assert.Equal(RiftRouletteTeams.Amber, state.Apply(Finished(RiftRouletteTeams.Amber)));
    Assert.Equal(RiftRouletteTeams.Sapphire, state.Apply(Finished(RiftRouletteTeams.Sapphire)));

    Assert.Equal("Sapphire 2 - 1 Amber", state.FormatScore());
  }

  [Theory]
  [InlineData(null)]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(4)]
  public void Finished_with_unknown_team_scores_nothing(int? team)
  {
    var state = new MatchState();

    Assert.Null(state.Apply(Finished(team)));
    Assert.Equal("Sapphire 0 - 0 Amber", state.FormatScore());
  }

  [Theory]
  [InlineData(RiftOutcome.Tied)]
  [InlineData(RiftOutcome.Cancelled)]
  [InlineData(RiftOutcome.SpawnTimedOut)]
  public void Non_finished_outcomes_score_nothing(string outcome)
  {
    var state = new MatchState();

    Assert.Null(state.Apply(new RiftRoundResult(outcome, RiftSide.Yellow, RiftRouletteTeams.Sapphire)));
    Assert.Equal("Sapphire 0 - 0 Amber", state.FormatScore());
  }

  [Fact]
  public void Tied_counts_a_tie()
  {
    var state = new MatchState();

    state.Apply(new RiftRoundResult(RiftOutcome.Tied, RiftSide.Green, null));
    state.Apply(new RiftRoundResult(RiftOutcome.Cancelled, RiftSide.Green, null));

    Assert.Equal(1, state.Ties);
  }

  [Fact]
  public void Reset_clears_everything()
  {
    var state = new MatchState();
    state.Start();
    state.BeginRound();
    state.Apply(Finished(RiftRouletteTeams.Amber));
    state.Apply(new RiftRoundResult(RiftOutcome.Tied, RiftSide.Green, null));

    state.Reset();

    Assert.Equal(MatchPhase.Idle, state.Phase);
    Assert.Equal(0, state.Round);
    Assert.Equal(0, state.Ties);
    Assert.Equal("Sapphire 0 - 0 Amber", state.FormatScore());
  }

  [Fact]
  public void Describe_result_names_the_winner_or_no_point()
  {
    Assert.Equal("Amber took the rift", MatchState.DescribeResult(Finished(RiftRouletteTeams.Amber), RiftRouletteTeams.Amber));
    Assert.Equal("Rift taken, winner unknown - no point", MatchState.DescribeResult(Finished(null), null));
    Assert.Equal("Tied - no point", MatchState.DescribeResult(new RiftRoundResult(RiftOutcome.Tied, null, null), null));
    Assert.Equal("Round cancelled - no point", MatchState.DescribeResult(new RiftRoundResult(RiftOutcome.Cancelled, null, null), null));
    Assert.Equal("Rift did not spawn - no point", MatchState.DescribeResult(new RiftRoundResult(RiftOutcome.SpawnTimedOut, null, null), null));
  }
}
