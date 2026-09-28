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

  // In fly cam with no target but far from the spot: the viewer flew there, the park did not fail.
  public static bool IsManualMove(bool roaming, bool hasTarget, float distance, float tolerance) =>
    roaming && !hasTarget && distance > tolerance;

  public static bool ManualActive(DateTime? until, DateTime now) => until is { } end && now < end;

  public static bool FollowReady(DateTime? spawnedAt, DateTime now, TimeSpan grace) =>
    spawnedAt is not { } spawned || now - spawned >= grace;
}
