using Bublock.Modules.Session;
using Bublock.Shared;
using DeadworksManaged.Api;

namespace __NAME__;

public class MatchPlugin : DeadworksPluginBase
{
  private static readonly Logger MatchLog = BublockLog.For("Match");

  public override string Name => "__TITLE__ Match";

  [GameEventHandler("player_death")]
  public HookResult OnPlayerDeath(PlayerDeathEvent args)
  {
    __NAME__Service.OnDeath(args, Timer);
    return HookResult.Continue;
  }

  // Game types never load together, so player commands like /points may repeat across them.
  [Command("points", Description = "Show this match's points, your place, and the time left")]
  public void CmdPoints(CCitadelPlayerController player)
  {
    foreach (var line in __NAME__Service.DescribeFor(player))
      PlayerChat.Send(player, line);
  }

  [Command("__PREFIX___status", Description = "__TITLE__: phase, match, time left, points, arena")]
  public void CmdStatus(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, MatchLog, "__PREFIX___status");

    foreach (var line in __NAME__Service.Describe())
      AdminCommand.Reply(caller, $"[__NAME__] {line}");
  }

  [Command("__PREFIX___start", Description = "Start a __TITLE__ match now, even with one player")]
  public void CmdStart(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, MatchLog, "__PREFIX___start");
    __NAME__Service.Session.Start(Timer, ExecutionMode.Debug);
    AdminCommand.Reply(caller, $"[__NAME__] Match {__NAME__Service.Session.Match} started.");
  }

  [Command("__PREFIX___end", Description = "End the current __TITLE__ match now and show the result")]
  public void CmdEnd(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, MatchLog, "__PREFIX___end");

    AdminCommand.Reply(caller, __NAME__Service.Session.End(Timer, ExecutionMode.Debug) is { } result
      ? $"[__NAME__] Match {result.Match} ended; {result.Winners.Count} winner(s)."
      : "[__NAME__] No match is running.");
  }

  [Command("__PREFIX___time", Description = "Set the __TITLE__ match length from the next match: __PREFIX___time <30-1800 seconds>")]
  public void CmdTime(CCitadelPlayerController? caller, int seconds)
  {
    AdminCommand.Authorize(caller, MatchLog, "__PREFIX___time");

    AdminCommand.Reply(caller, __NAME__Service.Session.TrySetMatchSeconds(seconds)
      ? $"[__NAME__] Match length {SessionRule.Clock(seconds)} from the next match."
      : $"[__NAME__] Length must be {SessionOptions.MinMatchSeconds} to {SessionOptions.MaxMatchSeconds} seconds.");
  }
}
