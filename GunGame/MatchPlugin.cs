using Bublock.Modules.Session;
using Bublock.Shared;
using DeadworksManaged.Api;

namespace GunGame;

public class MatchPlugin : DeadworksPluginBase
{
  private static readonly Logger MatchLog = BublockLog.For("Match");

  public override string Name => "Gun Game Match";

  [GameEventHandler("player_death")]
  public HookResult OnPlayerDeath(PlayerDeathEvent args)
  {
    GunGameService.OnDeath(args, Timer);
    return HookResult.Continue;
  }

  [Command("points", Description = "Show the Gun Game kills this match, your place, and the time left")]
  public void CmdPoints(CCitadelPlayerController player)
  {
    foreach (var line in GunGameService.DescribeFor(player))
      PlayerChat.Send(player, line);
  }

  [Command("gg_status", Description = "Gun Game: phase, match, time left, kills, arena, heroes")]
  public void CmdStatus(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, MatchLog, "gg_status");

    foreach (var line in GunGameService.Describe())
      AdminCommand.Reply(caller, $"[GunGame] {line}");
  }

  [Command("gg_start", Description = "Start a Gun Game match now, even with one player")]
  public void CmdStart(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, MatchLog, "gg_start");

    GunGameService.Session.Start(Timer, ExecutionMode.Debug);
    AdminCommand.Reply(caller, $"[GunGame] Match {GunGameService.Session.Match} started ({SessionRule.Clock(GunGameService.Session.Options.MatchSeconds)}).");
  }

  [Command("gg_end", Description = "End the current Gun Game match now and show the result")]
  public void CmdEnd(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, MatchLog, "gg_end");

    AdminCommand.Reply(caller, GunGameService.Session.End(Timer, ExecutionMode.Debug) is { } result
      ? $"[GunGame] Match {result.Match} ended; {result.Winners.Count} winner(s)."
      : "[GunGame] No match is running.");
  }

  [Command("gg_time", Description = "Set the Gun Game match length from the next match: gg_time <30-1800 seconds>")]
  public void CmdTime(CCitadelPlayerController? caller, int seconds)
  {
    AdminCommand.Authorize(caller, MatchLog, "gg_time");

    AdminCommand.Reply(caller, GunGameService.Session.TrySetMatchSeconds(seconds)
      ? $"[GunGame] Match length {SessionRule.Clock(seconds)} from the next match."
      : $"[GunGame] Length must be {SessionOptions.MinMatchSeconds} to {SessionOptions.MaxMatchSeconds} seconds.");
  }

  [Command("gg_reroll", Description = "Give a player a new random hero and build now, as a kill does: gg_reroll <slot>")]
  public void CmdReroll(CCitadelPlayerController? caller, int slot)
  {
    AdminCommand.Authorize(caller, MatchLog, "gg_reroll");

    var player = Players.GetAll().FirstOrDefault(candidate => candidate.Slot == slot)
      ?? throw new CommandException($"No player in slot {slot}.");

    AdminCommand.Reply(caller, $"[GunGame] {GunGameService.Reroll(player, Timer)}");
  }
}
