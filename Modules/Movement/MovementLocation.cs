using System.Numerics;

namespace Bublock.Modules.Movement;

public sealed record MovementLocation(string Name, Vector3 Position, Vector3 Angle)
{
  public MovementLocation Offset(Vector3 local, string? name = null)
  {
    var yaw = Angle.Y * MathF.PI / 180f;
    var forward = new Vector3(MathF.Cos(yaw), MathF.Sin(yaw), 0f);
    var right = new Vector3(MathF.Sin(yaw), -MathF.Cos(yaw), 0f);

    return this with
    {
      Name = name ?? Name,
      Position = Position + forward * local.X + right * local.Y + Vector3.UnitZ * local.Z
    };
  }
}
