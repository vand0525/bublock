using System.Numerics;
using Bublock.Modules.Movement;

namespace CrystalCrackers.Pit;

public static class PitLocations
{
  // Same spot and view as Rift Roulette's watch_center: the sky floor right above the pit.
  public static readonly MovementLocation Watch = new(
    "pit_watch",
    new Vector3(0f, 0f, 1536.062500f),
    new Vector3(-23f, 45f, 0f));

  // The pit floor (z -768) is rotationally symmetric; each team gets the flat end toward its base, facing the boss.
  public static readonly MovementLocation Sapphire = new(
    "pit_sapphire",
    new Vector3(60f, 500f, -768f),
    new Vector3(0f, -90f, 0f));

  public static readonly MovementLocation Amber = new(
    "pit_amber",
    new Vector3(-60f, -500f, -768f),
    new Vector3(0f, 90f, 0f));

  // Rift Roulette's mid spawns (center_*): mid lane toward each team's base, facing mid.
  public static readonly MovementLocation LaneSapphire = new(
    "lane_sapphire",
    new Vector3(0f, 2000f, 384f),
    new Vector3(0f, -90f, 0f));

  public static readonly MovementLocation LaneAmber = new(
    "lane_amber",
    new Vector3(0f, -2000f, 384f),
    new Vector3(0f, 90f, 0f));

  public static IReadOnlyList<MovementLocation> All { get; } = [Watch, Sapphire, Amber, LaneSapphire, LaneAmber];

  public static MovementLocation? PitFor(int team) => team switch
  {
    PitRoundRule.AmberTeam => Amber,
    PitRoundRule.SapphireTeam => Sapphire,
    _ => null
  };

  public static MovementLocation? LaneFor(int team) => team switch
  {
    PitRoundRule.AmberTeam => LaneAmber,
    PitRoundRule.SapphireTeam => LaneSapphire,
    _ => null
  };

  public static void RegisterAll()
  {
    foreach (var location in All)
      MovementService.Locations.Register(location);
  }
}
