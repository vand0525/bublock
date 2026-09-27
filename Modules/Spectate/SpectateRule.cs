using System.Numerics;

namespace Bublock.Modules.Spectate;

public enum SpectateReason
{
  Keep,
  Killer,
  Any,
  Park
}

public readonly record struct SpectateChoice(SpectateReason Reason, ulong? Target);

public static class SpectateRule
{
  // Source caps view pitch at 89; 90 would be clamped or flip the view.
  public const float StraightDownPitch = 89f;

  public static SpectateChoice Choose(ulong? currentId, ulong? killerId, IReadOnlyList<ulong> candidates)
  {
    if (currentId is { } current && candidates.Contains(current))
      return new SpectateChoice(SpectateReason.Keep, current);

    if (killerId is { } killer && candidates.Contains(killer))
      return new SpectateChoice(SpectateReason.Killer, killer);

    if (candidates.Count > 0)
      return new SpectateChoice(SpectateReason.Any, candidates[0]);

    return new SpectateChoice(SpectateReason.Park, null);
  }

  public static Vector3 LookDown(float yaw) => new(StraightDownPitch, yaw, 0f);

  public static bool ParkCheck(bool roaming, bool hasTarget, float distance, float tolerance) =>
    roaming && !hasTarget && distance <= tolerance;
}
