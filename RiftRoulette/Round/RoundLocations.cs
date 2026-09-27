using Bublock.Modules.Movement;
using RiftRoulette.Locations;
using RiftRoulette.Rift;

namespace RiftRoulette.Round;

public static class RoundLocations
{
  public static (MovementLocation Sapphire, MovementLocation Amber) StartsFor(RiftSide side) =>
    side == RiftSide.Green
      ? (RiftRouletteLocations.GreenSapphire, RiftRouletteLocations.GreenAmber)
      : (RiftRouletteLocations.YellowSapphire, RiftRouletteLocations.YellowAmber);

  public static MovementLocation WatchFor(RiftSide side) =>
    side == RiftSide.Green ? RiftRouletteLocations.WatchGreen : RiftRouletteLocations.WatchYellow;
}
