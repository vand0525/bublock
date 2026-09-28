using Bublock.Shared;
using DeadworksManaged.Api;

namespace RiftRoulette.Lobby;

public static class FlexSlots
{
  public const string TeamManager = "citadel_team_manager";

  public const ushort AllUnlocked = 15;

  private static readonly Logger Log = BublockLog.For("Lobby");

  private static bool _warnedMissing;

  public static SchemaAccessor<ushort> Unlocked => new(
    "CCitadelTeam"u8,
    "m_nFlexSlotsUnlocked"u8);

  public static int UnlockAll(ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var teams = Entities.ByDesignerName(TeamManager)
      .Where(team => team.TeamNum is RiftRouletteTeams.Amber or RiftRouletteTeams.Sapphire)
      .ToList();

    if (teams.Count == 0)
    {
      if (!_warnedMissing)
        log.Warn("Flex slots not unlocked, no team entity Name={Name}", TeamManager);

      _warnedMissing = true;
      return 0;
    }

    _warnedMissing = false;
    var accessor = Unlocked;

    if (accessor.GetAddress(teams[0].Handle) == teams[0].Handle)
    {
      log.Warn("Flex slots not unlocked, schema field missing Field={Field}", "CCitadelTeam.m_nFlexSlotsUnlocked");
      return 0;
    }

    foreach (var team in teams)
    {
      var before = accessor.Get(team.Handle);

      if (before == AllUnlocked)
        continue;

      accessor.Set(team.Handle, AllUnlocked);
      log.Info("Flex slots unlocked Team={Team} Before={Before} After={After}", RiftRouletteTeams.Name(team.TeamNum), before, accessor.Get(team.Handle));
    }

    log.Debug("Flex slots checked Teams={Teams}", teams.Count);
    return teams.Count;
  }

  public static IReadOnlyList<string> Describe()
  {
    var accessor = Unlocked;
    var teams = Entities.ByDesignerName(TeamManager).ToList();

    if (teams.Count == 0)
      return [$"No {TeamManager} entity found"];

    if (accessor.GetAddress(teams[0].Handle) == teams[0].Handle)
      return ["Schema field CCitadelTeam.m_nFlexSlotsUnlocked not found"];

    return teams
      .Select(team => $"Team={team.TeamNum} ({RiftRouletteTeams.Name(team.TeamNum)}) FlexSlotsUnlocked={accessor.Get(team.Handle)}")
      .ToList();
  }
}
