using RiftRoulette.GunGame;

namespace Bublock.Tests.RiftRoulette;

public class GunGameLadderTests
{
  [Fact]
  public void Kills_climb_and_the_first_to_the_target_wins()
  {
    var ladder = new GunGameLadder();
    ladder.TrySetTarget(3);

    Assert.Equal(new LadderStep(true, 1, false), ladder.Record(10, 20));
    Assert.Equal(new LadderStep(true, 1, false), ladder.Record(20, 10));
    Assert.Equal(new LadderStep(true, 2, false), ladder.Record(10, 20));
    Assert.Equal(new LadderStep(true, 3, true), ladder.Record(10, 30));
    Assert.Equal(10UL, ladder.Winner);
  }

  [Fact]
  public void No_kill_counts_after_a_win_or_for_a_self_kill()
  {
    var ladder = new GunGameLadder();
    ladder.TrySetTarget(1);
    ladder.Record(10, 20);

    Assert.Equal(new LadderStep(false, 0, false), ladder.Record(20, 10));
    Assert.Equal(0, ladder.KillsOf(20));

    ladder.Reset();

    Assert.Equal(new LadderStep(false, 0, false), ladder.Record(30, 30));
    Assert.Null(ladder.Winner);
  }

  [Theory]
  [InlineData(0, false)]
  [InlineData(1, true)]
  [InlineData(50, true)]
  [InlineData(51, false)]
  public void Target_must_be_in_range_and_survives_reset(int target, bool accepted)
  {
    var ladder = new GunGameLadder();

    Assert.Equal(accepted, ladder.TrySetTarget(target));

    ladder.Reset();
    Assert.Equal(accepted ? target : GunGameLadder.DefaultTarget, ladder.Target);
  }

  [Fact]
  public void Standings_and_places_order_by_kills_with_shared_places()
  {
    var ladder = new GunGameLadder();
    ladder.Record(30, 1);
    ladder.Record(10, 1);
    ladder.Record(10, 2);
    ladder.Record(20, 1);

    Assert.Equal([(10UL, 2), (20UL, 1), (30UL, 1)], ladder.Standings());
    Assert.Equal(1, ladder.PlaceOf(10));
    Assert.Equal(2, ladder.PlaceOf(20));
    Assert.Equal(2, ladder.PlaceOf(30));
    Assert.Equal(4, ladder.PlaceOf(99));

    ladder.Forget(10);
    Assert.Equal(1, ladder.PlaceOf(20));
  }
}
