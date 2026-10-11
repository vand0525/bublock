using Bublock.Modules.WorldText;
using System.Numerics;

namespace RiftRoulette.Boards;

public static class EmptySignLayout
{
  public const string TitleText = "RIFT ROULETTE";
  public const string JoinAddress = "66.94.102.245:27015";
  public const string JoinText = "Join with connect " + JoinAddress;

  // Where a spectator's camera starts on an empty server, captured with getpos_exact.
  public static readonly Vector3 CameraPosition = Vector3.Zero;
  public static readonly Vector3 CameraAngle = new(-0.59375f, 179.3125f, 0f);

  public const float Distance = 400f;
  public const float FontSize = 64f;
  public const float TitleScale = 1.2f;
  public const float JoinScale = 0.45f;
  public const float LineGap = 8f;

  // Text width per character as a share of the font size, estimated from the welcome board (WatchLayout.WelcomeHalfWidth).
  public const float CharWidth = 0.66f;

  // Dark text: the area under mid is bright beige, and white text washed out against it.
  private static readonly WorldTextColor TitleColor = new(15, 20, 45, 255);
  private static readonly WorldTextColor JoinColor = new(30, 30, 30, 255);

  public static Vector3 Angle => WorldTextPlacement.FacingViewer(CameraAngle.Y);

  // Positive pitch looks down, so the middle of the view sits lower.
  public static Vector3 Center =>
    WorldTextPlacement.InFrontOf(CameraPosition, CameraAngle.Y, Distance)
      - new Vector3(0f, 0f, MathF.Tan(CameraAngle.X * MathF.PI / 180f) * Distance);

  public static WorldTextSpec Title() => Line(TitleText, TitleScale, LineGap, TitleColor);

  public static WorldTextSpec Join() => Line(JoinText, JoinScale, -LineGap - FontSize * JoinScale, JoinColor);

  public static float Width(string text, float scale) => text.Length * CharWidth * FontSize * scale;

  // The viewer's right, the way world text runs.
  public static Vector3 Right(float yaw)
  {
    var radians = yaw * MathF.PI / 180f;
    return new Vector3(MathF.Sin(radians), -MathF.Cos(radians), 0f);
  }

  // A board's position is its text's bottom-left corner.
  private static WorldTextSpec Line(string text, float scale, float bottom, WorldTextColor color)
  {
    var position = Center - Right(CameraAngle.Y) * (Width(text, scale) / 2f) + new Vector3(0f, 0f, bottom);
    return new(text, position, Angle, color, scale, FontSize);
  }
}
