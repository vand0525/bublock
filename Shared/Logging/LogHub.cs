namespace Bublock.Shared;

public sealed class LogHub : IDisposable
{
  public const long DefaultMaxBytes = 10 * 1024 * 1024;
  public const string MasterFile = "master";

  private readonly object _gate = new();
  private readonly Dictionary<string, RollingFileWriter> _writers = new(StringComparer.OrdinalIgnoreCase);
  private readonly Dictionary<string, Logger> _features = new(StringComparer.OrdinalIgnoreCase);
  private readonly Func<DateTime> _utcNow;
  private readonly long _maxBytes;
  private readonly Action<string> _fallback;

  public LogHub(
    string dllTag,
    string directory,
    Func<DateTime>? utcNow = null,
    long maxBytes = DefaultMaxBytes,
    int retentionDays = LogRetention.DefaultDays,
    Action<string>? fallback = null)
  {
    DllTag = dllTag;
    Directory = directory;
    SessionId = Guid.NewGuid().ToString("N")[..8];
    _utcNow = utcNow ?? (() => DateTime.UtcNow);
    _maxBytes = maxBytes;
    _fallback = fallback ?? Console.WriteLine;

    Master = new Logger(this, feature: null, LogLevel.Information);

    try
    {
      System.IO.Directory.CreateDirectory(directory);
      LogRetention.DeleteOlderThan(directory, _utcNow(), retentionDays);
    }
    catch (Exception ex)
    {
      Disable(ex);
    }
  }

  public string DllTag { get; }
  public string Directory { get; }
  public string SessionId { get; }
  public string? RoundId { get; set; }
  public bool FileLoggingEnabled { get; private set; } = true;
  public Logger Master { get; }

  internal DateTime UtcNow => _utcNow();

  public Logger For(string feature)
  {
    lock (_gate)
    {
      if (!_features.TryGetValue(feature, out var logger))
      {
        logger = new Logger(this, feature, LogLevel.Information);
        _features[feature] = logger;
      }

      return logger;
    }
  }

  internal void Write(string fileBase, string line)
  {
    lock (_gate)
    {
      if (!FileLoggingEnabled)
        return;

      try
      {
        if (!_writers.TryGetValue(fileBase, out var writer))
        {
          writer = new RollingFileWriter(Directory, fileBase, _maxBytes, _utcNow);
          _writers[fileBase] = writer;
        }

        writer.WriteLine(line);
      }
      catch (Exception ex)
      {
        Disable(ex);
      }
    }
  }

  public void Dispose()
  {
    lock (_gate)
    {
      foreach (var writer in _writers.Values)
        writer.Dispose();

      _writers.Clear();
    }
  }

  private void Disable(Exception ex)
  {
    if (!FileLoggingEnabled)
      return;

    FileLoggingEnabled = false;

    foreach (var writer in _writers.Values)
      writer.Dispose();

    _writers.Clear();

    try
    {
      _fallback($"[Bublock.Logging] {DllTag}: file logging disabled ({Directory}): {ex.Message}");
    }
    catch
    {
    }
  }
}
