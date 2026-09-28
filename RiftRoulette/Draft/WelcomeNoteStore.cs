using Bublock.Shared;

namespace RiftRoulette.Draft;

public static class WelcomeNoteStore
{
  public const string FileName = "welcomenote.txt";

  private static readonly Logger Log = BublockLog.For("Draft");

  private static string? _text;

  public static string FilePath =>
    Path.Combine(Path.GetDirectoryName(LogPaths.ResolveRoot())!, FileName);

  public static string Text => _text ??= Load();

  public static void Set(string text, ExecutionMode mode = ExecutionMode.Clean)
  {
    _text = text.Trim();
    Write(_text, mode);
  }

  private static string Load()
  {
    var path = FilePath;

    try
    {
      if (!File.Exists(path))
        return "";

      var text = File.ReadAllText(path).Trim();
      Log.Info("Welcome note loaded Length={Length}", text.Length);
      return text;
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      Log.Error(exception, "Welcome note unreadable, drawing none Path={Path}", path);
      return "";
    }
  }

  private static void Write(string text, ExecutionMode mode)
  {
    var path = FilePath;

    try
    {
      File.WriteAllText(path, text);
      Log.WithMode(mode).Debug("Welcome note written Path={Path} Length={Length}", path, text.Length);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      Log.Error(exception, "Welcome note not written, kept for this load only Path={Path}", path);
    }
  }
}
