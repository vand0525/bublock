using Bublock.Shared;
using DeadworksManaged.Api;

namespace RiftRoulette.GunGame;

public class GunGamePlugin : DeadworksPluginBase
{
  private static readonly Logger GunGameLog = BublockLog.For("GunGame");

  public override string Name => "Rift Roulette Gun Game";

  [Command("ladder", Description = "Show the Gun Game kill ladder and your place")]
  public void CmdLadder(CCitadelPlayerController player)
  {
    foreach (var line in GunGameService.DescribePlayer(player))
      PlayerChat.Send(player, line);
  }

  [Command("gungame_status", Description = "Gun Game: on or off, target, winner, every player's kills")]
  public void CmdGunGameStatus(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, GunGameLog, "gungame_status");

    foreach (var line in GunGameService.Describe())
      AdminCommand.Reply(caller, $"[GunGame] {line}");
  }

  [Command("gungame_target", Description = "Set the Gun Game kill target between matches: gungame_target <1-50>")]
  public void CmdGunGameTarget(CCitadelPlayerController? caller, int kills)
  {
    AdminCommand.Authorize(caller, GunGameLog, "gungame_target");

    AdminCommand.Reply(caller, $"[GunGame] {GunGameService.SetTarget(kills, ExecutionMode.Debug)}");
  }

  [Command("gungame_reroll", Description = "Give a player a new random hero and build now, as a Gun Game kill does: gungame_reroll <slot>")]
  public void CmdGunGameReroll(CCitadelPlayerController? caller, int slot)
  {
    AdminCommand.Authorize(caller, GunGameLog, "gungame_reroll");

    var player = Players.GetAll().FirstOrDefault(candidate => candidate.Slot == slot)
      ?? throw new CommandException($"No player in slot {slot}.");

    AdminCommand.Reply(caller, $"[GunGame] {GunGameService.Reroll(player, Timer, ExecutionMode.Debug)}");
  }
}
