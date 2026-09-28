using System.Globalization;
using RiftRoulette.Stats;

namespace RiftRoulette.Betting;

public sealed record BetRow(string Name, int Chips);

public static class BetBoardText
{
  public const string Title = "BETTING";
  public const string Empty = "No souls yet";
  public const int MaxRows = 8;

  public static IReadOnlyList<BetRow> Rank(IEnumerable<BetRow> rows) =>
    rows
      .OrderByDescending(row => row.Chips)
      .ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
      .Take(MaxRows)
      .ToList();

  public static string Board(IEnumerable<BetRow> rows)
  {
    var ranked = Rank(rows);
    var lines = ranked.Count == 0
      ? [Empty]
      : ranked.Select((row, index) => $"{index + 1}  {StatsBoardText.Trim(row.Name)}   {Format(row.Chips)}").ToList();

    return string.Join("\n", new[] { Title, "" }.Concat(lines));
  }

  public static string Format(int chips) => chips.ToString("N0", CultureInfo.InvariantCulture);
}
