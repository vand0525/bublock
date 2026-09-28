using System.Numerics;

namespace Bublock.Modules.Movement;

public sealed record MovementLocation(string Name, Vector3 Position, Vector3 Angle)
{
  public MovementLocation Offset(Vector3 local, string? name = null)
  {
    var (forward, right) = Axes();

    return this with
    {
      Name = name ?? Name,
      Position = Position + forward * local.X + right * local.Y + Vector3.UnitZ * local.Z
    };
  }

  public Vector3 LocalOf(Vector3 world)
  {
    var (forward, right) = Axes();
    var delta = world - Position;

    return new Vector3(Vector3.Dot(delta, forward), Vector3.Dot(delta, right), delta.Z);
  }

  private (Vector3 Forward, Vector3 Right) Axes()
  {
    var yaw = Angle.Y * MathF.PI / 180f;
    return (new Vector3(MathF.Cos(yaw), MathF.Sin(yaw), 0f), new Vector3(MathF.Sin(yaw), -MathF.Cos(yaw), 0f));
  }
}
