using System.Globalization;
using System.Text;

namespace Bublock.Shared;

public sealed class RollingFileWriter : IDisposable
{
  private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

  private readonly string _directory;
  private readonly string _baseName;
  private readonly long _maxBytes;
  private readonly Func<DateTime> _utcNow;

  private StreamWriter? _writer;
  private string? _date;
  private int _index;

  public RollingFileWriter(string directory, string baseName, long maxBytes, Func<DateTime> utcNow)
  {
    _directory = directory;
    _baseName = baseName;
    _maxBytes = maxBytes;
    _utcNow = utcNow;
  }

  public string? CurrentPath { get; private set; }

  public void WriteLine(string line)
  {
    var date = _utcNow().ToUniversalTime().ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    if (_writer == null || date != _date)
      Open(date, startIndex: 0);
    else if (_writer.BaseStream.Length >= _maxBytes)
      Open(date, startIndex: _index + 1);

    _writer!.WriteLine(line);
  }

  public string PathFor(string date, int index) =>
    Path.Combine(
      _directory,
      index == 0 ? $"{_baseName}-{date}.log" : $"{_baseName}-{date}.{index}.log");

  public void Dispose()
  {
    _writer?.Dispose();
    _writer = null;
  }

  private void Open(string date, int startIndex)
  {
    _writer?.Dispose();
    _writer = null;

    Directory.CreateDirectory(_directory);

    var index = startIndex;

    while (File.Exists(PathFor(date, index)) && new FileInfo(PathFor(date, index)).Length >= _maxBytes)
      index++;

    var path = PathFor(date, index);

    var stream = new FileStream(
      path,
      FileMode.Append,
      FileAccess.Write,
      FileShare.ReadWrite | FileShare.Delete);

    _writer = new StreamWriter(stream, Utf8NoBom) { AutoFlush = true };
    _date = date;
    _index = index;
    CurrentPath = path;
  }
}
