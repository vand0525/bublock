namespace Shared.Tests;

internal sealed class TempDir : IDisposable
{
  public TempDir()
  {
    Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bublock-tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(Path);
  }

  public string Path { get; }

  public string[] Files() =>
    Directory.GetFiles(Path).Select(System.IO.Path.GetFileName).OrderBy(n => n).ToArray()!;

  public string Read(string fileName)
  {
    using var stream = new FileStream(
      System.IO.Path.Combine(Path, fileName),
      FileMode.Open,
      FileAccess.Read,
      FileShare.ReadWrite | FileShare.Delete);
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
  }

  public void Dispose()
  {
    try
    {
      Directory.Delete(Path, recursive: true);
    }
    catch (IOException)
    {
    }
  }
}
