namespace Bublock.Modules.Loadout;

public sealed record ProgressionLevel(int Souls, int Level, int Boons, int Unlocks, int AbilityPoints);

public static class Progression
{
  private static readonly (int Souls, bool Unlock)[] Thresholds =
  [
    (600, true),
    (800, false),
    (1100, true),
    (1500, false),
    (2000, true),
    (2600, false),
    (3200, false),
    (3800, true),
    (4500, false),
    (5200, false),
    (5900, false),
    (6600, false),
    (7400, false),
    (8200, false),
    (9000, false),
    (9800, false),
    (10700, false),
    (11600, false),
    (12500, false),
    (13400, false),
    (14400, false),
    (15500, false),
    (16700, false),
    (18000, false),
    (19500, false),
    (21200, false),
    (23100, false),
    (25200, false),
    (27500, false),
    (30000, false),
    (32700, false),
    (35600, false),
    (38700, false),
    (42000, false),
    (45500, false),
    (49200, false)
  ];

  private static readonly ProgressionLevel[] Levels = Build();

  public static int MaxLevel => Levels.Length;

  public static ProgressionLevel Max => Levels[^1];

  public static ProgressionLevel ForSouls(int souls)
  {
    var reached = Levels[0];

    foreach (var level in Levels)
    {
      if (level.Souls > souls)
        break;

      reached = level;
    }

    return reached;
  }

  private static ProgressionLevel[] Build()
  {
    var levels = new ProgressionLevel[Thresholds.Length];
    var unlocks = 0;
    var points = 0;

    for (var index = 0; index < Thresholds.Length; index++)
    {
      var (souls, unlock) = Thresholds[index];

      if (unlock)
        unlocks++;
      else
        points++;

      levels[index] = new ProgressionLevel(souls, index + 1, index, unlocks, points);
    }

    return levels;
  }
}
