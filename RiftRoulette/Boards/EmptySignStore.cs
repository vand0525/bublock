using System.Text.Json;
using Bublock.Shared;

namespace RiftRoulette.Boards;

public static class EmptySignStore
{
  public const string FileName = "emptysign.json";

  private static readonly Logger Log = BublockLog.For("Boards");

  private static EmptySignSettings? _current;

  public static string FilePath =>
    Path.Combine(Path.GetDirectoryName(LogPaths.ResolveRoot())!, FileName);

  public static EmptySignSettings Current => _current ??= Load();

  public static void Set(EmptySignSettings settings, ExecutionMode mode = ExecutionMode.Clean)
  {
    _current = settings;
    Write(settings, mode);
  }

  private static EmptySignSettings Load()
  {
    var path = FilePath;

    try
    {
      if (!File.Exists(path))
        return EmptySignSettings.Default;

      var settings = EmptySignSettings.FromJson(File.ReadAllText(path));

      if (settings.Problem() is { } problem)
      {
        Log.Warn("Empty sign settings out of range, using defaults Path={Path} Problem={Problem}", path, problem);
        return EmptySignSettings.Default;
      }

      Log.Info("Empty sign settings loaded Settings={Settings}", settings.Describe());
      return settings;
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
    {
      Log.Error(exception, "Empty sign settings unreadable, using defaults Path={Path}", path);
      return EmptySignSettings.Default;
    }
  }

  private static void Write(EmptySignSettings settings, ExecutionMode mode)
  {
    var path = FilePath;

    try
    {
      File.WriteAllText(path, settings.ToJson());
      Log.WithMode(mode).Debug("Empty sign settings written Path={Path}", path);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      Log.Error(exception, "Empty sign settings not written, kept for this load only Path={Path}", path);
    }
  }
}
