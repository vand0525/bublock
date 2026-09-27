using System.Numerics;

namespace RiftRoulette.Rift;

public enum RiftSide
{
  Green,
  Yellow
}

public static class RiftSides
{
  public static readonly Vector3 GreenPosition = new(7612f, -0.000661f, 444f);
  public static readonly Vector3 YellowPosition = new(-7560f, 0f, 424f);
  public static readonly Vector3 MiddlePosition = new(0f, 0f, 0f);

  public const float MatchDistance = 1000f;

  public static Vector3 Position(RiftSide side) =>
    side == RiftSide.Green ? GreenPosition : YellowPosition;

  public static RiftSide Other(RiftSide side) =>
    side == RiftSide.Green ? RiftSide.Yellow : RiftSide.Green;

  public static string Name(RiftSide side) =>
    side == RiftSide.Green ? "GREEN" : "YELLOW";

  public static bool TryMatch(Vector3 position, out RiftSide side)
  {
    foreach (var candidate in new[] { RiftSide.Green, RiftSide.Yellow })
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

  public static RiftSide Nearest(Vector3 position) =>
    Vector3.DistanceSquared(position, YellowPosition) < Vector3.DistanceSquared(position, GreenPosition)
      ? RiftSide.Yellow
      : RiftSide.Green;

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
      default:
        side = RiftSide.Green;
        return false;
    }
  }
}
