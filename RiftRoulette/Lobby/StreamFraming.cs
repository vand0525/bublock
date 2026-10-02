using System.Numerics;
using RiftRoulette.Rift;

namespace RiftRoulette.Lobby;

public static class StreamFraming
{
  // World position and view angle (pitch, yaw, roll), captured in fly cam with getpos_exact.
  public static (Vector3 Position, Vector3 Angle) Spot(RiftSide side) => side switch
  {
    RiftSide.Green => (new Vector3(7121.0f, -119.90625f, 2142.375f), new Vector3(12.28125f, -1.9375f, 0f)),
    RiftSide.Yellow => (new Vector3(-7063.34375f, 47.59375f, 1664.28125f), new Vector3(-4.75f, 174.84375f, 0f)),
    _ => (new Vector3(-479.6875f, 70.34375f, 1639.96875f), new Vector3(-13.25f, -8.625f, 0f))
  };
}
