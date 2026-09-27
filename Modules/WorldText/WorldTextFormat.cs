namespace Bublock.Modules.WorldText;

public static class WorldTextFormat
{
  public const int DefaultPreviewLength = 40;

  public static string FromArgs(IEnumerable<string> parts) =>
    string.Join(' ', parts).Replace("\\n", "\n");

  public static string Preview(string text, int maxLength = DefaultPreviewLength)
  {
    var singleLine = text.Replace("\r", "").Replace("\n", " / ");

    return singleLine.Length <= maxLength
      ? singleLine
      : singleLine[..maxLength] + "...";
  }
}
