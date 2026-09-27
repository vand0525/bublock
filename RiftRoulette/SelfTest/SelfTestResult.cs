namespace RiftRoulette.SelfTest;

public enum CheckStatus
{
  Pass,
  Warn,
  Fail
}

public sealed record CheckResult(string Area, string Name, CheckStatus Status, string Detail = "")
{
  public string Describe() =>
    $"{Label(Status)} [{Area}] {Name}{(Detail.Length > 0 ? $" - {Detail}" : "")}";

  public static string Label(CheckStatus status) => status switch
  {
    CheckStatus.Pass => "PASS",
    CheckStatus.Warn => "WARN",
    _ => "FAIL"
  };
}

public static class SelfTestReport
{
  public static string Summarize(IReadOnlyCollection<CheckResult> results) =>
    $"PASS {Count(results, CheckStatus.Pass)} | WARN {Count(results, CheckStatus.Warn)} | FAIL {Count(results, CheckStatus.Fail)}";

  public static IReadOnlyList<string> Lines(IReadOnlyCollection<CheckResult> results, bool all)
  {
    var lines = results
      .Where(result => all || result.Status != CheckStatus.Pass)
      .Select(result => result.Describe())
      .ToList();

    if (!all)
    {
      lines.AddRange(results
        .GroupBy(result => result.Area)
        .Select(area => $"{area.Key}: {Summarize(area.ToList())}"));
    }

    lines.Add($"Total: {Summarize(results)}");
    return lines;
  }

  private static int Count(IEnumerable<CheckResult> results, CheckStatus status) =>
    results.Count(result => result.Status == status);
}
