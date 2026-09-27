using Bublock.Shared;
using DeadworksManaged.Api;

namespace RiftRoulette.Stats;

public class StatsPlugin : DeadworksPluginBase
{
  private static readonly Logger StatsLog = BublockLog.For("Stats");

  public override string Name => "Rift Roulette Stats";

  [GameEventHandler("player_death")]
  public HookResult OnPlayerDeath(PlayerDeathEvent args)
  {
    StatsService.RecordDeath(args);
    return HookResult.Continue;
  }

  [Command("stats", Description = "Show your kills, deaths, and assists this match")]
  public void CmdStats(CCitadelPlayerController player)
  {
    foreach (var line in StatsService.Describe(player))
      PlayerChat.Send(player, line);
  }

  [Command("stats_board", Description = "Redraw the Sapphire and Amber stats boards (Random mode)")]
  public void CmdStatsBoard(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, StatsLog, "stats_board");

    StatsService.RefreshBoards(ExecutionMode.Debug);

    foreach (var line in StatsService.DescribeAll())
      AdminCommand.Reply(caller, $"[Stats] {line}");
  }

  [Command("stats_reset", Description = "Zero every player's match kills, deaths, and assists")]
  public void CmdStatsReset(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, StatsLog, "stats_reset");

    StatsService.Reset(ExecutionMode.Debug);
    AdminCommand.Reply(caller, "[Stats] Match stats reset");
  }
}
