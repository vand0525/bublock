using RiftRoulette.Rift;

namespace RiftRoulette.Round;

public static class WatchSpotRule
{
  public static RiftSide SideFor(bool riftRunning, RiftSide? currentSide, RiftSide nextSide) =>
    riftRunning && currentSide is { } current ? current : nextSide;
}
