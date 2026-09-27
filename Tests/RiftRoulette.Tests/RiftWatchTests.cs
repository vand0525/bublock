using RiftRoulette.Rift;

namespace Bublock.Tests.RiftRoulette;

public class RiftWatchTests
{
  [Fact]
  public void New_trooper_finishes_even_with_a_cashin()
  {
    var watch = new RiftWatch();

    Assert.Equal(RiftObservation.Finished, watch.Observe(newTrooper: true, cashinExists: true));
  }

  [Fact]
  public void First_cashin_sighting_is_reported_once()
  {
    var watch = new RiftWatch();

    Assert.Equal(RiftObservation.CashinAppeared, watch.Observe(false, true));
    Assert.Equal(RiftObservation.Waiting, watch.Observe(false, true));
    Assert.True(watch.SawCashin);
  }

  [Fact]
  public void Cashin_disappearing_after_being_seen_is_a_tie()
  {
    var watch = new RiftWatch();

    watch.Observe(false, true);

    Assert.Equal(RiftObservation.Tied, watch.Observe(false, false));
  }

  [Fact]
  public void No_cashin_ever_seen_keeps_waiting()
  {
    var watch = new RiftWatch();

    Assert.Equal(RiftObservation.Waiting, watch.Observe(false, false));
    Assert.Equal(RiftObservation.Waiting, watch.Observe(false, false));
    Assert.False(watch.SawCashin);
  }
}
