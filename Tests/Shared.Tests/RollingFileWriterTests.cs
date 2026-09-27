using Bublock.Shared;

namespace Shared.Tests;

public class RollingFileWriterTests
{
  [Fact]
  public void WriteLine_UsesDailyFileName()
  {
    using var dir = new TempDir();
    using var writer = new RollingFileWriter(dir.Path, "draft", 1024, () => new DateTime(2026, 9, 26, 23, 0, 0, DateTimeKind.Utc));

    writer.WriteLine("one");

    Assert.Equal(["draft-20260926.log"], dir.Files());
    Assert.Equal("one" + Environment.NewLine, dir.Read("draft-20260926.log"));
  }

  [Fact]
  public void WriteLine_RollsToNewFileOnNewUtcDay()
  {
    using var dir = new TempDir();
    var now = new DateTime(2026, 9, 26, 23, 59, 0, DateTimeKind.Utc);
    using var writer = new RollingFileWriter(dir.Path, "draft", 1024, () => now);

    writer.WriteLine("day one");
    now = now.AddMinutes(2);
    writer.WriteLine("day two");

    Assert.Equal(["draft-20260926.log", "draft-20260927.log"], dir.Files());
    Assert.Contains("day two", dir.Read("draft-20260927.log"));
  }

  [Fact]
  public void WriteLine_RollsBySize()
  {
    using var dir = new TempDir();
    using var writer = new RollingFileWriter(dir.Path, "rift", 20, () => new DateTime(2026, 9, 26, 0, 0, 0, DateTimeKind.Utc));

    writer.WriteLine("0123456789012345678901");
    writer.WriteLine("second");
    writer.WriteLine("third");

    Assert.Equal(["rift-20260926.1.log", "rift-20260926.log"], dir.Files());
    Assert.Equal("second" + Environment.NewLine + "third" + Environment.NewLine, dir.Read("rift-20260926.1.log"));
  }

  [Fact]
  public void WriteLine_AfterRestartAppendsToNewestNonFullFile()
  {
    using var dir = new TempDir();
    var clock = () => new DateTime(2026, 9, 26, 0, 0, 0, DateTimeKind.Utc);

    using (var first = new RollingFileWriter(dir.Path, "rift", 20, clock))
    {
      first.WriteLine("0123456789012345678901");
      first.WriteLine("second");
    }

    using var restarted = new RollingFileWriter(dir.Path, "rift", 20, clock);
    restarted.WriteLine("after restart");

    Assert.Equal(["rift-20260926.1.log", "rift-20260926.log"], dir.Files());
    Assert.Contains("after restart", dir.Read("rift-20260926.1.log"));
  }
}
