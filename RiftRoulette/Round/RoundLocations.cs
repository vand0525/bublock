using Bublock.Modules.Movement;
using RiftRoulette.Locations;
using RiftRoulette.Rift;

namespace RiftRoulette.Round;

public static class RoundLocations
{
  public static (MovementLocation Sapphire, MovementLocation Amber) StartsFor(RiftSide side) =>
    side switch
    {
      RiftSide.Yellow => (RiftRouletteLocations.YellowSapphire, RiftRouletteLocations.YellowAmber),
      RiftSide.Center => (RiftRouletteLocations.CenterSapphire, RiftRouletteLocations.CenterAmber),
      _ => (RiftRouletteLocations.GreenSapphire, RiftRouletteLocations.GreenAmber)
    };

  public static MovementLocation WatchFor(RiftSide side) =>
    side switch
    {
      RiftSide.Yellow => RiftRouletteLocations.WatchYellow,
      RiftSide.Center => RiftRouletteLocations.WatchCenter,
      _ => RiftRouletteLocations.WatchGreen
    };
}
