namespace RiftRoulette.Stats;

public sealed record StatsRow(string Name, PlayerStats Stats);

public sealed record StreakRow(string Name, int Best);

public static class StatsBoardText
{
  public const int NameLength = 16;

  public const string StreakTitle = "STREAKS";

  public const string NoStreaks = "No streaks yet";

  public static IReadOnlyList<StreakRow> RankStreaks(IEnumerable<StreakRow> rows) =>
    rows
      .Where(row => row.Best > 0)
      .OrderByDescending(row => row.Best)
      .ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
      .ToList();

  public static IReadOnlyList<string> StreakLines(IEnumerable<StreakRow> rows)
  {
    var ranked = RankStreaks(rows);

    return ranked.Count == 0
      ? [NoStreaks]
      : ranked.Select((row, index) => $"{index + 1}  {Trim(row.Name)}   {row.Best}").ToList();
  }

  public static string StreakBoard(IEnumerable<StreakRow> rows) =>
    string.Join("\n", new[] { StreakTitle, "" }.Concat(StreakLines(rows)));

  public static string TeamBoard(string teamName, int rounds, IReadOnlyList<StatsRow> rows)
  {
    var total = rows.Aggregate(
      PlayerStats.Empty,
      (sum, row) => new PlayerStats(sum.Kills + row.Stats.Kills, sum.Deaths + row.Stats.Deaths, sum.Assists + row.Stats.Assists));

    var lines = new List<string>
    {
      $"{teamName} - {rounds} {(rounds == 1 ? "round" : "rounds")}",
      $"K / D / A   {total.Line}",
      ""
    };

    if (rows.Count == 0)
      lines.Add("No players");

    lines.AddRange(
      rows
        .OrderByDescending(row => row.Stats.Kills)
        .ThenBy(row => row.Stats.Deaths)
        .ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
        .Select(row => $"{Trim(row.Name)}   {row.Stats.Line}"));

    return string.Join("\n", lines);
  }

  public static string Trim(string name) =>
    name.Length <= NameLength ? name : name[..NameLength];
}
