using System.Numerics;
using System.Text.Json;
using Bublock.Modules.Movement;
using Bublock.Modules.Spectate;
using RiftRoulette.Rift;

namespace RiftRoulette.Lobby;

// Offset is (forward, right, up) from the watch spot anchor; Yaw is relative to the anchor's yaw.
public sealed record CameraPose(Vector3 Offset, float Pitch, float Yaw);

public static class StreamFraming
{
  // Above the players' floor up top (z 1536), so the straight-down view covers the platform.
  public const float OverheadHeight = 264f;

  public static readonly CameraPose Default = new(new Vector3(0f, 0f, OverheadHeight), SpectateRule.StraightDownPitch, 0f);

  public static CameraPose Pick(IReadOnlyDictionary<RiftSide, CameraPose> saved, RiftSide side) =>
    saved.TryGetValue(side, out var pose) ? pose
      : saved.TryGetValue(RiftSides.Other(side), out var other) ? other
      : Default;

  public static (Vector3 Position, Vector3 Angle) ToWorld(MovementLocation anchor, CameraPose pose) =>
    (anchor.Offset(pose.Offset).Position, new Vector3(pose.Pitch, SpectateRule.WrapDegrees(anchor.Angle.Y + pose.Yaw), 0f));

  public static CameraPose FromWorld(MovementLocation anchor, Vector3 position, Vector3 angle) =>
    new(anchor.LocalOf(position), angle.X, SpectateRule.WrapDegrees(angle.Y - anchor.Angle.Y));

  public static Dictionary<RiftSide, CameraPose> Parse(string json)
  {
    var root = JsonSerializer.Deserialize<Dictionary<string, PoseJson>>(json) ?? [];
    var poses = new Dictionary<RiftSide, CameraPose>();

    foreach (var (name, pose) in root)
    {
      if (RiftSides.TryParse(name, out var side) && pose.Offset is { Length: 3 } offset)
        poses[side] = new CameraPose(new Vector3(offset[0], offset[1], offset[2]), pose.Pitch, pose.Yaw);
    }

    return poses;
  }

  public static string Serialize(IReadOnlyDictionary<RiftSide, CameraPose> poses) =>
    JsonSerializer.Serialize(
      poses.ToDictionary(
        entry => RiftSides.Name(entry.Key).ToLowerInvariant(),
        entry => new PoseJson([entry.Value.Offset.X, entry.Value.Offset.Y, entry.Value.Offset.Z], entry.Value.Pitch, entry.Value.Yaw)),
      new JsonSerializerOptions { WriteIndented = true });

  private sealed record PoseJson(
    [property: System.Text.Json.Serialization.JsonPropertyName("offset")] float[]? Offset,
    [property: System.Text.Json.Serialization.JsonPropertyName("pitch")] float Pitch,
    [property: System.Text.Json.Serialization.JsonPropertyName("yaw")] float Yaw);
}
