using Bublock.Modules.Session;

namespace Bublock.Tests.Modules;

public class SessionTests
{
  [Theory]
  [InlineData(SessionPhase.Waiting, 1, SessionAction.None)]
  [InlineData(SessionPhase.Waiting, 2, SessionAction.Start)]
  [InlineData(SessionPhase.Playing, 2, SessionAction.None)]
  [InlineData(SessionPhase.Playing, 1, SessionAction.Stop)]
  [InlineData(SessionPhase.Break, 1, SessionAction.Stop)]
  [InlineData(SessionPhase.Break, 5, SessionAction.None)]
  public void Decide_starts_at_min_players_and_stops_below(SessionPhase phase, int players, SessionAction expected)
  {
    Assert.Equal(expected, SessionRule.Decide(phase, players, minPlayers: 2));
  }

  [Theory]
  [InlineData(SessionPhase.Waiting, 5, SessionAction.None)]
  [InlineData(SessionPhase.Playing, 1, SessionAction.Stop)]
  public void Without_auto_start_a_waiting_session_never_starts_but_still_stops(SessionPhase phase, int players, SessionAction expected)
  {
    Assert.Equal(expected, SessionRule.Decide(phase, players, minPlayers: 2, autoStart: false));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(10)]
  public void A_paused_match_is_never_started_or_stopped_by_player_counts(int players)
  {
    Assert.Equal(SessionAction.None, SessionRule.Decide(SessionPhase.Paused, players, minPlayers: 2));
  }

  [Fact]
  public void Winners_are_everyone_tied_on_top_and_nobody_without_points()
  {
    Assert.Equal([10UL], SessionRule.Winners([(10, 5), (20, 3)]));
    Assert.Equal([10UL, 20UL], SessionRule.Winners([(10, 5), (20, 5), (30, 1)]));
    Assert.Empty(SessionRule.Winners([]));
  }

  [Theory]
  [InlineData(120, "2:00")]
  [InlineData(7, "0:07")]
  [InlineData(-3, "0:00")]
  public void Clock_formats_minutes_and_seconds(int seconds, string expected)
  {
    Assert.Equal(expected, SessionRule.Clock(seconds));
  }

  [Theory]
  [InlineData(29, false)]
  [InlineData(30, true)]
  [InlineData(1800, true)]
  [InlineData(1801, false)]
  public void Match_length_must_be_in_range(int seconds, bool valid)
  {
    Assert.Equal(valid, SessionRule.IsValidMatchSeconds(seconds));
  }

  [Fact]
  public void Scoreboard_adds_ranks_and_forgets()
  {
    var board = new Scoreboard();

    Assert.Equal(1, board.Add(10));
    Assert.Equal(2, board.Add(10));
    board.Add(20);
    board.Add(30);

    Assert.Equal([(10UL, 2), (20UL, 1), (30UL, 1)], board.Standings());
    Assert.Equal(1, board.PlaceOf(10));
    Assert.Equal(2, board.PlaceOf(30));
    Assert.Equal(4, board.PlaceOf(99));

    board.Forget(10);
    Assert.Equal(1, board.PlaceOf(20));

    board.Clear();
    Assert.Equal(0, board.Count);
  }
}
