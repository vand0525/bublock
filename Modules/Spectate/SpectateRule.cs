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

  public const float MoveUnits = 50f;

  public const float TurnDegrees = 3f;

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

  public static bool FollowReady(DateTime? spawnedAt, DateTime now, TimeSpan grace) =>
    spawnedAt is not { } spawned || now - spawned >= grace;

  public static float Turned(Vector3 from, Vector3 to) =>
    MathF.Max(MathF.Abs(to.X - from.X), MathF.Abs(WrapDegrees(to.Y - from.Y)));

  public static float WrapDegrees(float degrees)
  {
    var wrapped = (degrees + 180f) % 360f;
    return (wrapped < 0f ? wrapped + 360f : wrapped) - 180f;
  }

  public static bool HandMoved(float distance, float turned) =>
    distance > MoveUnits || turned > TurnDegrees;
}
