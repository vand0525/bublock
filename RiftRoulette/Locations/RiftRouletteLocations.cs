using System.Numerics;
using Bublock.Modules.Movement;

namespace RiftRoulette.Locations;

public static class RiftRouletteLocations
{
  public static readonly MovementLocation Draft = new(
    "draft",
    new Vector3(0f, 0, 1536.062500f),
    new Vector3(0.000000f, 0, 0.000000f));

  public static readonly MovementLocation WatchGreen = new(
    "watch_green",
    new Vector3(7612f, 0f, 1536.062500f),
    new Vector3(-23f, 45f, 0f));

  public static readonly MovementLocation WatchYellow = new(
    "watch_yellow",
    new Vector3(-7560f, 0f, 1536.062500f),
    new Vector3(-23f, -135f, 0f));

  public static readonly MovementLocation GreenSapphire = new(
    "green_sapphire",
    new Vector3(8225.000000f, 1797.718750f, 248.062500f),
    new Vector3(0.000000f, -100.343750f, 0.000000f));

  public static readonly MovementLocation GreenAmber = new(
    "green_amber",
    new Vector3(7025.656250f, -2088.593750f, 256.031250f),
    new Vector3(0.000000f, 80.718750f, 0.000000f));

  public static readonly MovementLocation YellowSapphire = new(
    "yellow_sapphire",
    new Vector3(-7072.656250f, 2209.531250f, 248.031250f),
    new Vector3(0.000000f, -102.156250f, 0.000000f));

  public static readonly MovementLocation YellowAmber = new(
    "yellow_amber",
    new Vector3(-8259.562500f, -2134.312500f, 248.031250f),
    new Vector3(0.000000f, 80.875000f, 0.000000f));

  public static IReadOnlyList<MovementLocation> All { get; } =
    [Draft, WatchGreen, WatchYellow, GreenSapphire, GreenAmber, YellowSapphire, YellowAmber];

  public static void RegisterAll()
  {
    foreach (var location in All)
      MovementService.Locations.Register(location);
  }
}
