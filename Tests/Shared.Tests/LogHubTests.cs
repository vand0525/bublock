using Bublock.Shared;

namespace Shared.Tests;

public class LogHubTests
{
  private static readonly DateTime Now = new(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);

  [Fact]
  public void FeatureLogger_WritesOwnFileWithFeaturePrefix()
  {
    using var dir = new TempDir();
    using var hub = new LogHub("RiftRoulette", dir.Path, () => Now);

    hub.For("Draft").Info(new PlayerRef(3, 76561198192980843, "Theo"), "Selected hero {Hero}", "Shiv");

    Assert.Equal(["draft-20260926.log"], dir.Files());

    var line = dir.Read("draft-20260926.log").TrimEnd();
    Assert.Equal(
      $"2026-09-26T12:00:00.000Z [Information] [RiftRoulette.Draft] session={hub.SessionId} round=- player=\"Theo\" steam=76561198192980843 slot=3 | Selected hero Hero=Shiv",
      line);
  }

  [Fact]
  public void MasterLogger_UsesDllPrefix()
  {
    using var dir = new TempDir();
    using var hub = new LogHub("CleanSlate", dir.Path, () => Now);

    hub.Master.Info("Loaded");

    Assert.Contains("[Information] [CleanSlate] session=", dir.Read("master-20260926.log"));
  }

  [Fact]
  public void FeatureWarning_IsCopiedToMasterWithPointer()
  {
    using var dir = new TempDir();
    using var hub = new LogHub("RiftRoulette", dir.Path, () => Now);

    hub.For("Rift").Info("Spawned");
    hub.For("Rift").Error(new InvalidOperationException("bad"), "Spawn failed {Side}", "GREEN");

    var master = dir.Read("master-20260926.log");
    Assert.Contains("[Error] [RiftRoulette.Rift]", master);
    Assert.Contains("Spawn failed Side=GREEN (see rift.log)", master);
    Assert.DoesNotContain("Spawned", master);
    Assert.DoesNotContain("InvalidOperationException", master);
    Assert.Contains("InvalidOperationException: bad", dir.Read("rift-20260926.log"));
  }

  [Fact]
  public void CleanMode_DropsDebugAndTrace_DebugModeKeepsThem()
  {
    using var dir = new TempDir();
    using var hub = new LogHub("RiftRoulette", dir.Path, () => Now);

    var clean = hub.For("Draft");
    clean.Debug("clean debug");
    clean.Trace("clean trace");

    var debug = clean.WithMode(ExecutionMode.Debug);
    debug.Debug("debug debug");
    debug.Trace("debug trace");

    var text = dir.Read("draft-20260926.log");
    Assert.DoesNotContain("clean", text);
    Assert.Contains("[Debug] [RiftRoulette.Draft]", text);
    Assert.Contains("[Trace] [RiftRoulette.Draft]", text);
    Assert.Equal(LogLevel.Information, clean.WithMode(ExecutionMode.Clean).MinimumLevel);
  }

  [Fact]
  public void RoundId_AppearsOnceSet()
  {
    using var dir = new TempDir();
    using var hub = new LogHub("RiftRoulette", dir.Path, () => Now);

    hub.RoundId = "r1";
    hub.Master.Info("Round started");

    Assert.Contains("round=r1 |", dir.Read("master-20260926.log"));
  }

  [Fact]
  public void For_ReturnsCachedLoggerCaseInsensitive()
  {
    using var dir = new TempDir();
    using var hub = new LogHub("RiftRoulette", dir.Path, () => Now);

    Assert.Same(hub.For("Draft"), hub.For("draft"));
    Assert.Equal(8, hub.SessionId.Length);
  }

  [Fact]
  public void Constructor_RunsRetention()
  {
    using var dir = new TempDir();
    File.WriteAllText(Path.Combine(dir.Path, "master-20260101.log"), "old");

    using var hub = new LogHub("RiftRoulette", dir.Path, () => Now);

    Assert.Empty(dir.Files());
  }

  [Fact]
  public void WriteFailure_DisablesLoggingWithSingleFallbackLine()
  {
    using var dir = new TempDir();
    var blocker = Path.Combine(dir.Path, "blocked");
    File.WriteAllText(blocker, "not a directory");
    var fallback = new List<string>();

    using var hub = new LogHub("RiftRoulette", Path.Combine(blocker, "logs"), () => Now, fallback: fallback.Add);

    hub.Master.Info("one");
    hub.For("Draft").Error("two");

    Assert.False(hub.FileLoggingEnabled);
    Assert.Single(fallback);
    Assert.StartsWith("[Bublock.Logging] RiftRoulette: file logging disabled", fallback[0]);
  }
}
