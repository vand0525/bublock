using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.GameLoop;
using RiftRoulette.RandomMode;

namespace RiftRoulette.Balance;

public class BalancePlugin : DeadworksPluginBase
{
  private static readonly Logger BalanceLog = BublockLog.For("Balance");

  public override string Name => "Rift Roulette Balance";

  [Command("balance_status", Description = "Show auto-balance counters since the last swap and the current verdict")]
  public void CmdBalanceStatus(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, BalanceLog, "balance_status");

    foreach (var line in BalanceService.Describe())
      AdminCommand.Reply(caller, $"[Balance] {line}");
  }

  [Command("balance_auto", Description = "Turn auto-balance on or off: balance_auto <on|off>")]
  public void CmdBalanceAuto(CCitadelPlayerController? caller, string state)
  {
    AdminCommand.Authorize(caller, BalanceLog, "balance_auto");

    var enabled = state.ToLowerInvariant() switch
    {
      "on" => true,
      "off" => false,
      _ => throw new CommandException("Use balance_auto on or balance_auto off.")
    };

    BalanceService.SetEnabled(enabled, ExecutionMode.Debug);
    AdminCommand.Reply(caller, $"[Balance] Auto-balance {(enabled ? "on" : "off")}");
  }

  [Command("balance_now", Description = "Swap the leading team's best player now and reroll heroes (Random mode intermission)")]
  public void CmdBalanceNow(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, BalanceLog, "balance_now");

    if (!MatchConfig.IsRandom || MatchService.State.Phase != MatchPhase.Intermission)
      throw new CommandException("balance_now works only during a Random mode intermission.");

    var swapped = RandomModeService.PrepareRound(Timer, ExecutionMode.Debug, forceBalance: true);
    AdminCommand.Reply(caller, $"[Balance] Balanced and rerolled: {swapped} swapped, {RandomModeService.PendingCount} pending");
  }
}
