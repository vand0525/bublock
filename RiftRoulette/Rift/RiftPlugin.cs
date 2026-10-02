using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Round;

namespace RiftRoulette.Rift;

public class RiftPlugin : DeadworksPluginBase
{
  private static readonly Logger RiftLog = BublockLog.For("Rift");

  public override string Name => "Rift Roulette Rift";

  [Command("rift_start", Description = "Start the next rift (green/yellow/center rotation)")]
  public void CmdRiftStart(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, RiftLog, "rift_start");

    AdminCommand.Reply(caller, $"[Rift] {RoundFlow.RunRound(Timer, ExecutionMode.Debug)}");
  }

  [Command("rift_status", Description = "Show rift phase, current and next side, last outcome")]
  public void CmdRiftStatus(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, RiftLog, "rift_status");

    foreach (var line in RiftService.DescribeRift())
      AdminCommand.Reply(caller, $"[Rift] {line}");
  }

  [Command("rift_mid", Description = "Enable or disable the center rift: rift_mid <on|off>")]
  public void CmdRiftMid(CCitadelPlayerController? caller, string state)
  {
    AdminCommand.Authorize(caller, RiftLog, "rift_mid");

    var enabled = state.Trim().ToLowerInvariant() switch
    {
      "on" or "1" => true,
      "off" or "0" => false,
      _ => throw new CommandException("Use on or off.")
    };

    RiftService.SetMiddleEnabled(enabled, ExecutionMode.Debug);
    AdminCommand.Reply(caller, $"[Rift] Mid rift {(enabled ? "on" : "off")}. Next={RiftSides.Name(RiftService.NextSide)}.");
  }

  [Command("rift_next", Description = "Set the next rift side: rift_next <green|yellow|center>")]
  public void CmdRiftNext(CCitadelPlayerController? caller, string side)
  {
    AdminCommand.Authorize(caller, RiftLog, "rift_next");

    if (!RiftSides.TryParse(side, out var nextSide))
      throw new CommandException($"Unknown side '{side}'. Use green, yellow, or center.");

    if (nextSide == RiftSide.Center && !RiftService.MiddleEnabled)
      throw new CommandException("Mid rift is off. Use /rift_mid on first.");

    if (!RiftService.SetNextSide(nextSide, ExecutionMode.Debug))
      throw new CommandException($"A rift is running (Phase={RiftService.Phase}). Wait for it to end or use /rift_cancel.");

    var moved = WatchSpot.MoveAllUp(ExecutionMode.Debug);
    AdminCommand.Reply(caller, $"[Rift] Next rift: {RiftSides.Name(nextSide)}. {moved} player(s) moved to the watch spot.");
  }

  [Command("rift_cancel", Description = "End the running rift round and send players up top (a spawned rift stays)")]
  public void CmdRiftCancel(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, RiftLog, "rift_cancel");

    AdminCommand.Reply(caller, $"[Rift] {RoundFlow.CancelRound(Timer, ExecutionMode.Debug)}");
  }

  [Command("rift_cleanup", Description = "Remove every rift trooper on the map")]
  public void CmdRiftCleanup(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, RiftLog, "rift_cleanup");

    var removed = RiftService.CleanupRiftTroopers(ExecutionMode.Debug);

    AdminCommand.Reply(caller, $"[Rift] Removed {removed} trooper(s)");
  }
}
