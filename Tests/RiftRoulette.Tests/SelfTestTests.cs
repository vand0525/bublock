using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using RiftRoulette.SelfTest;

namespace RiftRoulette.Tests;

public class SelfTestTests
{
  private static readonly CheckResult[] Sample =
  [
    new("Convars", "a", CheckStatus.Pass, "value 1"),
    new("Convars", "b", CheckStatus.Warn, "value 2, expected 1"),
    new("Schema", "c", CheckStatus.Fail, "field not found (offset 0)"),
    new("Schema", "d", CheckStatus.Pass)
  ];

  [Fact]
  public void Describe_LabelsStatusAreaAndDetail()
  {
    Assert.Equal("WARN [Convars] b - value 2, expected 1", Sample[1].Describe());
    Assert.Equal("PASS [Schema] d", Sample[3].Describe());
  }

  [Fact]
  public void Summarize_CountsEachStatus() =>
    Assert.Equal("PASS 2 | WARN 1 | FAIL 1", SelfTestReport.Summarize(Sample));

  [Fact]
  public void Lines_ShortForm_ShowsOnlyProblemsThenAreaTotals()
  {
    var lines = SelfTestReport.Lines(Sample, all: false);

    Assert.Equal(
      [
        "WARN [Convars] b - value 2, expected 1",
        "FAIL [Schema] c - field not found (offset 0)",
        "Convars: PASS 1 | WARN 1 | FAIL 0",
        "Schema: PASS 1 | WARN 0 | FAIL 1",
        "Total: PASS 2 | WARN 1 | FAIL 1"
      ],
      lines);
  }

  [Fact]
  public void Lines_All_ShowsEveryCheck() =>
    Assert.Equal(Sample.Length + 1, SelfTestReport.Lines(Sample, all: true).Count);

  [Fact]
  public void EventCounters_CountPerName()
  {
    var name = $"test_event_{Guid.NewGuid():N}";

    Assert.Equal(0, EventCounters.Count(name));
    EventCounters.Hit(name);
    EventCounters.Hit(name);
    Assert.Equal(2, EventCounters.Count(name));
  }

  [Fact]
  public void Dependencies_HaveNoDuplicates()
  {
    Assert.Equal(GameDependencies.ConVars.Count, GameDependencies.ConVars.Select(c => c.Name).Distinct().Count());
    Assert.Equal(GameDependencies.Events.Count, GameDependencies.Events.Distinct().Count());
    Assert.Empty(GameDependencies.KeptEntities.Intersect(GameDependencies.CleanedEntities));
  }

  [Fact]
  public void Dependencies_ListEveryConvarTheSourceSets()
  {
    var root = BublockRoot();
    var pattern = new Regex("""(?:ConVar\.Find|ServerConVars\.TrySet)\("([a-z_]+)"|ExecuteCommand\("([a-z_]+) """);
    var listed = GameDependencies.ConVars.Select(c => c.Name).ToHashSet();

    var used = new[] { "Shared", "Modules", "RiftRoulette", "CleanSlate", "DevTools" }
      .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, folder), "*.cs", SearchOption.AllDirectories))
      .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
      .SelectMany(path => pattern.Matches(File.ReadAllText(path)))
      .Select(match => match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value)
      .Where(name => name != "sv_cheats")
      .ToHashSet();

    Assert.True(used.Count >= 15, $"source scan found only {used.Count} convars");
    Assert.Empty(used.Except(listed));
  }

  [Fact]
  public void Dependencies_CleanedEntitiesMatchCleanSlate()
  {
    var source = File.ReadAllText(Path.Combine(BublockRoot(), "CleanSlate", "CleanSlateService.cs"));
    var removed = Regex.Match(source, @"RemovedDesignerNames =\s*\[(.*?)\]", RegexOptions.Singleline).Groups[1].Value;
    var names = Regex.Matches(removed, "\"([a-z0-9_]+)\"").Select(match => match.Groups[1].Value);

    Assert.Equal(names.Order(), GameDependencies.CleanedEntities.Order());
  }

  private static string BublockRoot([CallerFilePath] string path = "") =>
    Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", ".."));
}
