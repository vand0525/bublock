using System.Numerics;

namespace RiftRoulette.Rift;

public enum RiftSide
{
  Green,
  Yellow,
  Center
}

public static class RiftSides
{
  public static readonly Vector3 GreenPosition = new(7612f, -0.000661f, 444f);
  public static readonly Vector3 YellowPosition = new(-7560f, 0f, 424f);
  // Approx side-rift KOTH height; no map info_koth_spawn_location at mid — tune after a live spawn.
  public static readonly Vector3 MiddlePosition = new(0f, 0f, 448f);

  public const float MatchDistance = 1000f;

  public static IReadOnlyList<RiftSide> All { get; } = [RiftSide.Green, RiftSide.Yellow, RiftSide.Center];

  public static Vector3 Position(RiftSide side) => side switch
  {
    RiftSide.Green => GreenPosition,
    RiftSide.Yellow => YellowPosition,
    RiftSide.Center => MiddlePosition,
    _ => GreenPosition
  };

  public static RiftSide Other(RiftSide side) =>
    side == RiftSide.Green ? RiftSide.Yellow : RiftSide.Green;

  /// <summary>
  /// Next side after a successful spawn. Mid on: Green → Yellow → Center → Green.
  /// Mid off: Green ↔ Yellow (Center advances to Green).
  /// </summary>
  public static RiftSide NextInRotation(RiftSide spawnedSide, bool middleEnabled)
  {
    if (!middleEnabled)
      return spawnedSide == RiftSide.Center ? RiftSide.Green : Other(spawnedSide);

    return spawnedSide switch
    {
      RiftSide.Green => RiftSide.Yellow,
      RiftSide.Yellow => RiftSide.Center,
      _ => RiftSide.Green
    };
  }

  public static string Name(RiftSide side) => side switch
  {
    RiftSide.Green => "GREEN",
    RiftSide.Yellow => "YELLOW",
    RiftSide.Center => "CENTER",
    _ => "GREEN"
  };

  public static bool TryMatch(Vector3 position, out RiftSide side)
  {
    foreach (var candidate in All)
    {
      if (Vector3.Distance(position, Position(candidate)) <= MatchDistance)
      {
        side = candidate;
        return true;
      }
    }

    side = RiftSide.Green;
    return false;
  }

  public static RiftSide Nearest(Vector3 position)
  {
    var best = RiftSide.Green;
    var bestDist = float.MaxValue;

    foreach (var candidate in All)
    {
      var dist = Vector3.DistanceSquared(position, Position(candidate));

      if (dist >= bestDist)
        continue;

      bestDist = dist;
      best = candidate;
    }

    return best;
  }

  public static bool TryParse(string? name, out RiftSide side)
  {
    switch (name?.Trim().ToLowerInvariant())
    {
      case "green":
        side = RiftSide.Green;
        return true;
      case "yellow":
        side = RiftSide.Yellow;
        return true;
      case "center":
      case "middle":
      case "mid":
        side = RiftSide.Center;
        return true;
      default:
        side = RiftSide.Green;
        return false;
    }
  }
}
