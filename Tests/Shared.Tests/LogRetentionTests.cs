using Bublock.Shared;

namespace Shared.Tests;

public class LogRetentionTests
{
  [Fact]
  public void DeleteOlderThan_RemovesOnlyDatedLogsPastCutoff()
  {
    using var dir = new TempDir();

    foreach (var name in new[]
             {
               "master-20260926.log",
               "master-20260919.log",
               "master-20260918.log",
               "draft-20260901.2.log",
               "notes.log",
               "master-20260901.txt"
             })
      File.WriteAllText(Path.Combine(dir.Path, name), "x");

    var deleted = LogRetention.DeleteOlderThan(dir.Path, new DateTime(2026, 9, 26, 8, 0, 0, DateTimeKind.Utc));

    Assert.Equal(2, deleted);
    Assert.Equal(
      ["master-20260901.txt", "master-20260919.log", "master-20260926.log", "notes.log"],
      dir.Files());
  }

  [Fact]
  public void DeleteOlderThan_MissingDirectoryReturnsZero()
  {
    var missing = Path.Combine(Path.GetTempPath(), "bublock-tests", Guid.NewGuid().ToString("N"));

    Assert.Equal(0, LogRetention.DeleteOlderThan(missing, DateTime.UtcNow));
  }
}
