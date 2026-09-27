namespace Bublock.Shared;

public sealed class Logger
{
  private readonly LogHub _hub;
  private readonly string? _feature;

  internal Logger(LogHub hub, string? feature, LogLevel minimumLevel)
  {
    _hub = hub;
    _feature = feature;
    MinimumLevel = minimumLevel;
    Prefix = feature == null ? hub.DllTag : $"{hub.DllTag}.{feature}";
    FileBase = feature == null ? LogHub.MasterFile : feature.ToLowerInvariant();
  }

  public string Prefix { get; }
  public string FileBase { get; }
  public LogLevel MinimumLevel { get; }

  public Logger WithMode(ExecutionMode mode) =>
    new(_hub, _feature, mode == ExecutionMode.Debug ? LogLevel.Trace : LogLevel.Information);

  public bool IsEnabled(LogLevel level) => level >= MinimumLevel;

  public void Trace(string template, params object?[] args) => Log(LogLevel.Trace, null, null, template, args);
  public void Trace(PlayerRef player, string template, params object?[] args) => Log(LogLevel.Trace, player, null, template, args);

  public void Debug(string template, params object?[] args) => Log(LogLevel.Debug, null, null, template, args);
  public void Debug(PlayerRef player, string template, params object?[] args) => Log(LogLevel.Debug, player, null, template, args);

  public void Info(string template, params object?[] args) => Log(LogLevel.Information, null, null, template, args);
  public void Info(PlayerRef player, string template, params object?[] args) => Log(LogLevel.Information, player, null, template, args);

  public void Warn(string template, params object?[] args) => Log(LogLevel.Warning, null, null, template, args);
  public void Warn(PlayerRef player, string template, params object?[] args) => Log(LogLevel.Warning, player, null, template, args);

  public void Error(string template, params object?[] args) => Log(LogLevel.Error, null, null, template, args);
  public void Error(Exception exception, string template, params object?[] args) => Log(LogLevel.Error, null, exception, template, args);
  public void Error(PlayerRef player, Exception? exception, string template, params object?[] args) => Log(LogLevel.Error, player, exception, template, args);

  public void Critical(string template, params object?[] args) => Log(LogLevel.Critical, null, null, template, args);
  public void Critical(Exception exception, string template, params object?[] args) => Log(LogLevel.Critical, null, exception, template, args);
  public void Critical(PlayerRef player, Exception? exception, string template, params object?[] args) => Log(LogLevel.Critical, player, exception, template, args);

  public void Log(LogLevel level, PlayerRef? player, Exception? exception, string template, params object?[] args)
  {
    if (!IsEnabled(level))
      return;

    try
    {
      var now = _hub.UtcNow;
      var message = LogFormatter.RenderTemplate(template, args);

      _hub.Write(
        FileBase,
        LogFormatter.Format(now, level, Prefix, _hub.SessionId, _hub.RoundId, player, message, exception));

      if (_feature != null && level >= LogLevel.Warning)
      {
        _hub.Write(
          LogHub.MasterFile,
          LogFormatter.Format(now, level, Prefix, _hub.SessionId, _hub.RoundId, player, $"{message} (see {FileBase}.log)"));
      }
    }
    catch
    {
    }
  }
}
