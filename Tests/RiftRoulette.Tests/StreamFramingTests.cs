using System.Numerics;
using RiftRoulette.Lobby;
using RiftRoulette.Rift;

namespace RiftRoulette.Tests;

public class StreamFramingTests
{
  [Fact]
  public void Green_spot_is_the_captured_fly_cam_view()
  {
    Assert.Equal((new Vector3(7121.0f, -119.90625f, 2142.375f), new Vector3(12.28125f, -1.9375f, 0f)), StreamFraming.Spot(RiftSide.Green));
  }

  [Fact]
  public void Yellow_spot_is_the_captured_fly_cam_view()
  {
    Assert.Equal((new Vector3(-7063.34375f, 47.59375f, 1664.28125f), new Vector3(-4.75f, 174.84375f, 0f)), StreamFraming.Spot(RiftSide.Yellow));
  }

  [Fact]
  public void Center_spot_is_the_captured_fly_cam_view()
  {
    Assert.Equal((new Vector3(-479.6875f, 70.34375f, 1639.96875f), new Vector3(-13.25f, -8.625f, 0f)), StreamFraming.Spot(RiftSide.Center));
  }
}
