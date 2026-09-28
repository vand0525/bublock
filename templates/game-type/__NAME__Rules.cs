using Bublock.Modules.Session;
using Bublock.Modules.Teams;

namespace __NAME__;

// __TITLE__'s own rules. The scaffold starts as timed team deathmatch in an arena: change these first.
public static class __NAME__Rules
{
  public const string Title = "__TITLE__";

  public const string ArenaResource = "__NAME__.arena.json";

  public static readonly SessionOptions Session = new(MatchSeconds: 120, BreakSeconds: 5, MinPlayers: 2, WarningSeconds: 10);

  // What scores a point. Starting rule: a player kills someone on the other team.
  public static bool Credits(ulong attacker, ulong victim, int attackerTeam, int victimTeam) =>
    attacker != 0
    && attacker != victim
    && DeadlockTeams.IsPlayable(attackerTeam)
    && attackerTeam != victimTeam;

  public static (string Title, string Description) StartBanner(int matchSeconds) =>
    (Title, $"{SessionRule.Clock(matchSeconds)} - most points wins");

  public static (string Title, string Description) WarningBanner(int secondsLeft) =>
    ($"{secondsLeft} seconds left", "Most points wins");

  public static (string Title, string Description) ResultBanner(MatchResult result, Func<ulong, string> nameOf, int breakSeconds)
  {
    var next = $"next match in {breakSeconds}s";

    if (result.Winners.Count == 0)
      return ("Match over", $"No points - {next}");

    var top = result.Standings[0].Points;
    var names = string.Join(", ", result.Winners.Select(nameOf));

    return result.Winners.Count == 1
      ? ($"{names} wins", $"{top} points - {next}")
      : ($"Tie: {names}", $"{top} points each - {next}");
  }

  public static string WaitingLine(int players, int minPlayers) =>
    $"{Title} starts at {minPlayers} players ({players} on).";
}
