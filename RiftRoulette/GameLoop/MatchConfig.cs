namespace RiftRoulette.GameLoop;

public enum HeroMode
{
  Random,
  Mirror
}

public enum MatchFormat
{
  Continuous
}

public static class MatchConfig
{
  public static HeroMode HeroMode { get; private set; } = HeroMode.Random;

  public static MatchFormat Format { get; private set; } = MatchFormat.Continuous;

  public static bool IsRandom => HeroMode == HeroMode.Random;

  public static bool IsMirror => HeroMode == HeroMode.Mirror;

  public static void SetHeroMode(HeroMode mode) => HeroMode = mode;

  public static void SetFormat(MatchFormat format) => Format = format;

  public static bool TryParseHeroMode(string text, out HeroMode mode) => TryParseName(text, out mode);

  public static bool TryParseFormat(string text, out MatchFormat format) => TryParseName(text, out format);

  public static string Names<T>() where T : struct, Enum =>
    string.Join("|", Enum.GetNames<T>().Select(name => name.ToLowerInvariant()));

  public static string Describe() => Describe(HeroMode, Format);

  public static string Describe(HeroMode mode, MatchFormat format) =>
    $"Mode={mode.ToString().ToLowerInvariant()} | Format={format.ToString().ToLowerInvariant()}";

  private static bool TryParseName<T>(string text, out T value) where T : struct, Enum
  {
    value = default;
    var trimmed = text.Trim();

    return trimmed.Length > 0
      && !char.IsDigit(trimmed[0])
      && trimmed[0] != '-'
      && Enum.TryParse(trimmed, true, out value)
      && Enum.IsDefined(value);
  }
}
