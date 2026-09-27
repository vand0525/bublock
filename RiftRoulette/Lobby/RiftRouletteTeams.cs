namespace RiftRoulette.Lobby;

public static class RiftRouletteTeams
{
  public const int Amber = 2;

  public const int Sapphire = 3;

  public static bool TryParse(string name, out int team)
  {
    team = name.ToLowerInvariant() switch
    {
      "sapphire" => Sapphire,
      "amber" => Amber,
      _ => 0
    };

    return team != 0;
  }

  public static bool IsPlayable(int team) => team is Sapphire or Amber;

  public static int Other(int team) => team == Sapphire ? Amber : Sapphire;

  public static string Name(int team) => team switch
  {
    Sapphire => "Sapphire",
    Amber => "Amber",
    _ => $"Team{team}"
  };
}
