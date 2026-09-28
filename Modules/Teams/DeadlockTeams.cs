namespace Bublock.Modules.Teams;

public static class DeadlockTeams
{
  public const int Spectator = 1;
  public const int Amber = 2;
  public const int Sapphire = 3;

  public static bool IsPlayable(int team) => team is Amber or Sapphire;

  public static int Other(int team) => team == Sapphire ? Amber : Sapphire;

  public static string Name(int team) => team switch
  {
    Amber => "Amber",
    Sapphire => "Sapphire",
    Spectator => "Spectator",
    _ => $"Team {team}"
  };

  // The team with fewer players; a tie is a coin flip. Non-playable teams in the input are ignored.
  public static int SmallerTeam(IEnumerable<int> teams, Random rng)
  {
    var list = teams.ToList();
    var amber = list.Count(team => team == Amber);
    var sapphire = list.Count(team => team == Sapphire);

    if (amber != sapphire)
      return amber < sapphire ? Amber : Sapphire;

    return rng.Next(2) == 0 ? Amber : Sapphire;
  }
}
