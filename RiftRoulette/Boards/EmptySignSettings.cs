using System.Text.Json;
using System.Text.Json.Serialization;
using Bublock.Modules.WorldText;

namespace RiftRoulette.Boards;

public sealed record SignColor(int R, int G, int B)
{
  public WorldTextColor ToWorldText() => new((byte)R, (byte)G, (byte)B, 255);

  public override string ToString() => $"{R} {G} {B}";
}

public sealed record EmptySignSettings
{
  public const string DefaultTitle = "RIFT ROULETTE";
  public const string DefaultJoin = "Join with connect " + EmptySignLayout.JoinAddress;

  public const float MinDistance = 50f;
  public const float MaxDistance = 3000f;
  public const float MaxOffset = 3000f;
  public const float MinScale = 0.05f;
  public const float MaxScale = 10f;
  public const float MaxGap = 500f;
  public const int MaxTextLength = 120;

  private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

  [JsonPropertyName("distance")] public float Distance { get; init; } = 300f;

  // Along the camera's right; negative is left.
  [JsonPropertyName("shift")] public float Shift { get; init; } = 100f;

  // Up from the middle of the view; negative is down.
  [JsonPropertyName("height")] public float Height { get; init; }

  // Per line, on top of the whole sign's position: right (negative: left) and up (negative: down).
  [JsonPropertyName("titleRight")] public float TitleRight { get; init; }
  [JsonPropertyName("titleUp")] public float TitleUp { get; init; }
  [JsonPropertyName("joinRight")] public float JoinRight { get; init; }
  [JsonPropertyName("joinUp")] public float JoinUp { get; init; }

  [JsonPropertyName("titleScale")] public float TitleScale { get; init; } = 1.2f;
  [JsonPropertyName("joinScale")] public float JoinScale { get; init; } = 0.45f;
  [JsonPropertyName("gap")] public float Gap { get; init; } = 8f;
  [JsonPropertyName("titleColor")] public SignColor TitleColor { get; init; } = new(0, 190, 0);
  [JsonPropertyName("joinColor")] public SignColor JoinColor { get; init; } = new(0, 190, 0);
  [JsonPropertyName("titleText")] public string TitleText { get; init; } = DefaultTitle;
  [JsonPropertyName("joinText")] public string JoinText { get; init; } = DefaultJoin;

  public static EmptySignSettings Default { get; } = new();

  // Null when every value is in range, else what is wrong.
  public string? Problem()
  {
    if (Distance is < MinDistance or > MaxDistance)
      return $"Distance must be {MinDistance}-{MaxDistance} units.";

    if (new[] { Shift, Height, TitleRight, TitleUp, JoinRight, JoinUp }.Any(offset => Math.Abs(offset) > MaxOffset))
      return $"Shift, height and line offsets must be within -{MaxOffset} to {MaxOffset} units.";

    if (TitleScale is < MinScale or > MaxScale || JoinScale is < MinScale or > MaxScale)
      return $"Sizes must be {MinScale}-{MaxScale} world units per pixel.";

    if (Gap is < 0f or > MaxGap)
      return $"Gap must be 0-{MaxGap} units.";

    if (!InRange(TitleColor) || !InRange(JoinColor))
      return "Colors are three numbers 0-255 (red green blue).";

    if (TitleText.Length > MaxTextLength || JoinText.Length > MaxTextLength)
      return $"Text must be at most {MaxTextLength} characters.";

    return null;
  }

  public string Describe() =>
    $"Distance={Distance} Shift={Shift} Height={Height} | TitleOffset={TitleRight} {TitleUp} JoinOffset={JoinRight} {JoinUp} | " +
    $"TitleSize={TitleScale} JoinSize={JoinScale} Gap={Gap} | " +
    $"TitleColor={TitleColor} JoinColor={JoinColor} | Title=\"{TitleText}\" Join=\"{JoinText}\"";

  public string ToJson() => JsonSerializer.Serialize(this, Json);

  // Missing fields keep their defaults; null or unreadable JSON throws.
  public static EmptySignSettings FromJson(string json) =>
    JsonSerializer.Deserialize<EmptySignSettings>(json, Json) ?? throw new JsonException("Empty sign settings are null.");

  private static bool InRange(SignColor? color) =>
    color is { R: >= 0 and <= 255, G: >= 0 and <= 255, B: >= 0 and <= 255 };
}
