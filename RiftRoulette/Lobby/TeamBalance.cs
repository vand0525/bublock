namespace RiftRoulette.Lobby;

public static class TeamBalance
{
  public static IReadOnlyDictionary<ulong, int> Even(IReadOnlyDictionary<ulong, int> current, Random rng)
  {
    var teams = new Dictionary<ulong, int>();
    var unplaced = new List<ulong>();

    foreach (var (steamId, team) in current.OrderBy(pair => pair.Key))
    {
      if (RiftRouletteTeams.IsPlayable(team))
        teams[steamId] = team;
      else
        unplaced.Add(steamId);
    }

    foreach (var steamId in unplaced.OrderBy(_ => rng.Next()))
      teams[steamId] = SmallerTeam(teams.Values, rng);

    while (true)
    {
      var sapphire = teams.Values.Count(team => team == RiftRouletteTeams.Sapphire);
      var amber = teams.Values.Count(team => team == RiftRouletteTeams.Amber);

      if (Math.Abs(sapphire - amber) <= 1)
        break;

      var bigger = sapphire > amber ? RiftRouletteTeams.Sapphire : RiftRouletteTeams.Amber;
      var candidates = teams.Where(pair => pair.Value == bigger).Select(pair => pair.Key).OrderBy(id => id).ToList();

      teams[candidates[rng.Next(candidates.Count)]] = RiftRouletteTeams.Other(bigger);
    }

    return teams;
  }

  public static int SmallerTeam(IEnumerable<int> teams, Random rng)
  {
    var list = teams.ToList();
    var sapphire = list.Count(team => team == RiftRouletteTeams.Sapphire);
    var amber = list.Count(team => team == RiftRouletteTeams.Amber);

    if (sapphire != amber)
      return sapphire < amber ? RiftRouletteTeams.Sapphire : RiftRouletteTeams.Amber;

    return rng.Next(2) == 0 ? RiftRouletteTeams.Sapphire : RiftRouletteTeams.Amber;
  }
}
