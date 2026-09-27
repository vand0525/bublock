using Bublock.Shared;
using DeadworksManaged.Api;

namespace RiftRoulette.SelfTest;

public class SelfTestPlugin : DeadworksPluginBase
{
  private static readonly Logger SelfTestLog = BublockLog.For("SelfTest");

  public override string Name => "Rift Roulette Self-Test";

  [Command("selftest_run", Description = "Check every game dependency (convars, schema, entities, heroes, items, floors, events): selftest_run [all]")]
  public void CmdSelfTestRun(CCitadelPlayerController? caller, string detail = "")
  {
    AdminCommand.Authorize(caller, SelfTestLog, "selftest_run");

    var results = SelfTestService.Run(ExecutionMode.Debug);

    foreach (var line in SelfTestReport.Lines(results, all: detail.Equals("all", StringComparison.OrdinalIgnoreCase)))
      AdminCommand.Reply(caller, $"[SelfTest] {line}");
  }

  [Command("selftest_live", Description = "Teleport, restrain, banner and loadout check on one player, between rounds: selftest_live <slot>")]
  public void CmdSelfTestLive(CCitadelPlayerController? caller, int slot)
  {
    AdminCommand.Authorize(caller, SelfTestLog, "selftest_live");

    var player = Players.GetAll().FirstOrDefault(player => player.Slot == slot)
      ?? throw new CommandException($"No player in slot {slot}.");

    AdminCommand.Reply(caller, $"[SelfTest] {SelfTestService.Live(caller, player, Timer, ExecutionMode.Debug)}");
  }
}
