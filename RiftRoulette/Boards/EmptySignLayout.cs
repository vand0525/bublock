using Bublock.Modules.WorldText;
using System.Numerics;

namespace RiftRoulette.Boards;

public static class EmptySignLayout
{
  public const string JoinAddress = "162.250.125.66:11074";

  // Where a spectator's camera starts on an empty server, captured with getpos_exact.
  public static readonly Vector3 CameraPosition = Vector3.Zero;
  public static readonly Vector3 CameraAngle = new(-0.59375f, 179.3125f, 0f);

  public const float FontSize = 64f;

  // Text width per character as a share of the font size, estimated from the welcome board (WatchLayout.WelcomeHalfWidth).
  public const float CharWidth = 0.66f;

  public static Vector3 Angle => WorldTextPlacement.FacingViewer(CameraAngle.Y);

  // Positive pitch looks down, so the middle of the view sits lower.
  public static Vector3 Center(EmptySignSettings settings) =>
    WorldTextPlacement.InFrontOf(CameraPosition, CameraAngle.Y, settings.Distance)
      - new Vector3(0f, 0f, MathF.Tan(CameraAngle.X * MathF.PI / 180f) * settings.Distance)
      + Right(CameraAngle.Y) * settings.Shift
      + new Vector3(0f, 0f, settings.Height);

  public static WorldTextSpec Title(EmptySignSettings settings) =>
    Line(settings, settings.TitleText, settings.TitleScale, settings.Gap, settings.TitleColor, settings.TitleRight, settings.TitleUp);

  public static WorldTextSpec Join(EmptySignSettings settings) =>
    Line(settings, settings.JoinText, settings.JoinScale, -settings.Gap - FontSize * settings.JoinScale, settings.JoinColor, settings.JoinRight, settings.JoinUp);

  public static float Width(string text, float scale) => text.Length * CharWidth * FontSize * scale;

  // The viewer's right, the way world text runs.
  public static Vector3 Right(float yaw)
  {
    var radians = yaw * MathF.PI / 180f;
    return new Vector3(MathF.Sin(radians), -MathF.Cos(radians), 0f);
  }

  // A board's position is its text's bottom-left corner.
  private static WorldTextSpec Line(EmptySignSettings settings, string text, float scale, float bottom, SignColor color, float right, float up)
  {
    var position = Center(settings)
      + Right(CameraAngle.Y) * (right - Width(text, scale) / 2f)
      + new Vector3(0f, 0f, bottom + up);
    return new(text, position, Angle, color.ToWorldText(), scale, FontSize);
  }
}
