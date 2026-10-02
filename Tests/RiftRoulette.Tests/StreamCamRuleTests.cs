using RiftRoulette.Lobby;

namespace RiftRoulette.Tests;

public class StreamCamRuleTests
{
  private static readonly TimeSpan Every = TimeSpan.FromSeconds(6);
  private static readonly TimeSpan Recent = TimeSpan.FromSeconds(2);

  private static ParkAction Step(bool flyCam, bool parkedForSide = true, bool placed = false, bool handled = false, bool moved = false, TimeSpan? since = null) =>
    StreamCamRule.ParkStep(flyCam, parkedForSide, placed, handled, moved, since, Every);

  [Fact]
  public void Outside_fly_cam_parks_for_a_new_side_then_reparks_every_interval()
  {
    Assert.Equal(ParkAction.Park, Step(flyCam: false, parkedForSide: false, since: Recent));
    Assert.Equal(ParkAction.Stay, Step(flyCam: false, since: Recent));
    Assert.Equal(ParkAction.Repark, Step(flyCam: false, since: Every));
  }

  [Fact]
  public void In_fly_cam_never_moves_the_camera_while_the_viewer_moves_it()
  {
    Assert.Equal(ParkAction.Stay, Step(flyCam: true, parkedForSide: false, moved: true));
    Assert.Equal(ParkAction.Stay, Step(flyCam: true, moved: true, since: Every));
  }

  [Fact]
  public void In_fly_cam_parks_for_a_new_side()
  {
    Assert.Equal(ParkAction.Park, Step(flyCam: true, parkedForSide: false, placed: true, handled: true, since: Recent));
  }

  [Fact]
  public void In_fly_cam_a_landed_or_handled_camera_stays()
  {
    Assert.Equal(ParkAction.Stay, Step(flyCam: true, placed: true, since: Every));
    Assert.Equal(ParkAction.Stay, Step(flyCam: true, handled: true, since: Every));
  }

  [Fact]
  public void In_fly_cam_a_park_that_did_not_land_is_resent_after_the_interval()
  {
    Assert.Equal(ParkAction.Stay, Step(flyCam: true, since: Recent));
    Assert.Equal(ParkAction.Repark, Step(flyCam: true, since: Every));
    Assert.Equal(ParkAction.Repark, Step(flyCam: true, since: null));
  }
}
