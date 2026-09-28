using System.Globalization;
using Bublock.Modules.Session;
using Bublock.Modules.Teams;

namespace GunGame;

public static class GunGameRules
{
  public const string Title = "Gun Game";

  public const string ArenaResource = "GunGame.arena.json";

  public static readonly SessionOptions Session = new(MatchSeconds: 120, BreakSeconds: 5, MinPlayers: 2, WarningSeconds: 10);

  // A point: a human on a playing team kills someone on the other team (no self kills, no team kills).
  public static bool Credits(ulong attacker, ulong victim, int attackerTeam, int victimTeam) =>
    attacker != 0
    && attacker != victim
    && DeadlockTeams.IsPlayable(attackerTeam)
    && attackerTeam != victimTeam;

  public static (string Title, string Description) StartBanner(int matchSeconds) =>
    (Title, $"{SessionRule.Clock(matchSeconds)} - most kills wins");

  public static (string Title, string Description) WarningBanner(int secondsLeft) =>
    ($"{secondsLeft} seconds left", "Most kills wins");

  public static (string Title, string Description) KillBanner(int kills, string heroName, string buildName, int souls) =>
    ($"{Kills(kills)}: {heroName}", BuildDescription(buildName, souls));

  public static (string Title, string Description) HeroBanner(string heroName, string buildName, int souls) =>
    (heroName, BuildDescription(buildName, souls));

  public static (string Title, string Description) ResultBanner(MatchResult result, Func<ulong, string> nameOf, int breakSeconds)
  {
    var next = $"next match in {breakSeconds}s";

    if (result.Winners.Count == 0)
      return ("Match over", $"No kills - {next}");

    var top = result.Standings[0].Points;
    var names = string.Join(", ", result.Winners.Select(nameOf));

    return result.Winners.Count == 1
      ? ($"{names} wins", $"{Kills(top)} - {next}")
      : ($"Tie: {names}", $"{Kills(top)} each - {next}");
  }

  public static string WaitingLine(int players, int minPlayers) =>
    $"Gun Game starts at {minPlayers} players ({players} on). Kill to get a new hero; most kills in 2 minutes wins.";

  public static string BuildDescription(string buildName, int souls) =>
    $"{buildName} - {souls.ToString("N0", CultureInfo.InvariantCulture)} souls";

  public static string Kills(int kills) => kills == 1 ? "1 kill" : $"{kills} kills";
}
