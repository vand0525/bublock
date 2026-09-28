namespace RiftRoulette.GameLoop;

public enum HeroMode
{
  Random,
  Draft,
  Duel
}

public enum MatchFormat
{
  Continuous,
  GunGame
}

public static class MatchConfig
{
  public static HeroMode HeroMode { get; private set; } = HeroMode.Random;

  public static MatchFormat Format { get; private set; } = MatchFormat.Continuous;

  public const string DuelAlias = "1v1";

  public static bool IsRandom => HeroMode == HeroMode.Random;

  public static bool IsDuel => HeroMode == HeroMode.Duel;

  public static bool UsesDraft => HeroMode == HeroMode.Draft;

  // Gun Game (Stage 13k) rides on Random mode: the format only applies with random heroes.
  public static bool IsGunGame => Format == MatchFormat.GunGame && IsRandom;

  public static void SetHeroMode(HeroMode mode) => HeroMode = mode;

  public static void SetFormat(MatchFormat format) => Format = format;

  public static bool TryParseHeroMode(string text, out HeroMode mode)
  {
    if (string.Equals(text.Trim(), DuelAlias, StringComparison.OrdinalIgnoreCase))
    {
      mode = HeroMode.Duel;
      return true;
    }

    return TryParseName(text, out mode);
  }

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
