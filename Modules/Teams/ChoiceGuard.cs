namespace Bublock.Modules.Teams;

public static class ChoiceGuard
{
  public const string SelectHero = "selecthero";
  public const string ChangeTeam = "changeteam";
  public const string JoinTeam = "jointeam";

  // Which client console commands a game type stops (OnClientConCommand -> HookResult.Stop).
  public static bool Blocks(string command, bool heroLocked, bool teamLocked)
  {
    var name = command.Trim().ToLowerInvariant();

    return (heroLocked && name == SelectHero)
      || (teamLocked && name is ChangeTeam or JoinTeam);
  }
}
