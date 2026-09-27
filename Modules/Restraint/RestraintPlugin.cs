using Bublock.Shared;
using DeadworksManaged.Api;

namespace Bublock.Modules.Restraint;

public class RestraintPlugin : DeadworksPluginBase
{
  private static readonly Logger CommandsLog = BublockLog.For("Restraint");

  public override string Name => "Restraint";

  public override void OnGameFrame(bool simulating, bool firstTick, bool lastTick)
  {
    if (simulating)
      RestraintService.Sustain();
  }

  [Command("restrain", Description = "Silence, disarm and block melee for a player until released: restrain <slot>")]
  public void CmdRestrain(CCitadelPlayerController? caller, int slot)
  {
    AdminCommand.Authorize(caller, CommandsLog, "restrain");

    var player = BySlot(slot);
    var added = RestraintService.Restrain(player, ExecutionMode.Debug);
    AdminCommand.Reply(caller, $"[Restraint] {player.PlayerName} {(added ? "restrained" : "was already restrained")}");
  }

  [Command("restrain_release", Description = "Remove silence, disarm and the melee block from a player: restrain_release <slot>")]
  public void CmdRelease(CCitadelPlayerController? caller, int slot)
  {
    AdminCommand.Authorize(caller, CommandsLog, "restrain_release");

    var player = BySlot(slot);
    var removed = RestraintService.Release(player, ExecutionMode.Debug);
    AdminCommand.Reply(caller, $"[Restraint] {player.PlayerName} {(removed ? "released" : "was not restrained")}");
  }

  [Command("restrain_list", Description = "List restrained players and their active restraint modifiers")]
  public void CmdList(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, CommandsLog, "restrain_list");

    foreach (var line in RestraintService.Describe())
      AdminCommand.Reply(caller, $"[Restraint] {line}");
  }

  [Command("status_add", Description = "Test a game modifier by name on a player: status_add <slot> <modifier> [seconds]")]
  public void CmdStatusAdd(CCitadelPlayerController? caller, int slot, string modifier, float seconds = 10f)
  {
    AdminCommand.Authorize(caller, CommandsLog, "status_add");

    var player = BySlot(slot);
    var pawn = player.GetHeroPawn();

    if (pawn == null || !pawn.IsAlive)
      throw new CommandException($"{player.PlayerName} has no live pawn.");

    var added = RestraintService.AddModifier(pawn, modifier, seconds);
    CommandsLog.WithMode(ExecutionMode.Debug).Info(player.ToPlayerRef(), "Modifier test Modifier={Modifier} Seconds={Seconds} Added={Added}", modifier, seconds, added);
    AdminCommand.Reply(caller, $"[Restraint] {modifier} on {player.PlayerName} for {seconds}s: {(added ? "added" : "refused (unknown name?)")}");
  }

  [Command("status_remove", Description = "Remove a game modifier by name from a player: status_remove <slot> <modifier>")]
  public void CmdStatusRemove(CCitadelPlayerController? caller, int slot, string modifier)
  {
    AdminCommand.Authorize(caller, CommandsLog, "status_remove");

    var player = BySlot(slot);
    var removed = player.GetHeroPawn()?.RemoveModifier(modifier) ?? false;
    AdminCommand.Reply(caller, $"[Restraint] {modifier} on {player.PlayerName}: {(removed ? "removed" : "not found")}");
  }

  private static CCitadelPlayerController BySlot(int slot) =>
    Players.GetAll().FirstOrDefault(candidate => candidate.Slot == slot)
      ?? throw new CommandException($"No player in slot {slot}.");
}
