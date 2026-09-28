using Bublock.Shared;
using DeadworksManaged.Api;

namespace GunGame;

// Dev / prod: /play, /stop, /pause (owner's names, no prefix) and the dev-only environment tools.
public class DevPlugin : DeadworksPluginBase
{
  private static readonly Logger DevLog = BublockLog.For("DevMode");

  public override string Name => "Gun Game Dev";

  [Command("play", Description = "Start the live session (prod) from dev, or resume a paused match (admin)")]
  public void CmdPlay(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, DevLog, "play");
    AdminCommand.Reply(caller, $"[GunGame] {GunGameService.Play(Timer)}");
  }

  [Command("stop", Description = "Stop the live session and go back to dev, where everyone stood (admin)")]
  public void CmdStop(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, DevLog, "stop");
    AdminCommand.Reply(caller, $"[GunGame] {GunGameService.Stop(Timer)}");
  }

  [Command("pause", Description = "Pause the live match and write a debug snapshot (admin)")]
  public void CmdPause(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, DevLog, "pause");

    foreach (var line in GunGameService.Pause(Timer))
      AdminCommand.Reply(caller, $"[GunGame] {line}");
  }

  [Command("gg_bots", Description = "Dev: practice bots for solo testing, gg_bots <0-11> (0 kicks them); bots then count as players")]
  public void CmdBots(CCitadelPlayerController? caller, int count)
  {
    AdminCommand.Authorize(caller, DevLog, "gg_bots");
    GunGameService.RequireDev("gg_bots");
    AdminCommand.Reply(caller, $"[GunGame] {GunGameService.SetBots(count, Timer)}");
  }

  [Command("gg_map", Description = "Dev: reload the current map (spawn settings like practice bots); players stay connected")]
  public void CmdMap(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, DevLog, "gg_map");
    GunGameService.RequireDev("gg_map");
    AdminCommand.Reply(caller, $"[GunGame] {GunGameService.ReloadMap()}");
  }

  [Command("gg_exec", Description = "Dev: run a server console command from in game (logged), gg_exec <command ...>")]
  public void CmdExec(CCitadelPlayerController? caller, params string[] command)
  {
    AdminCommand.Authorize(caller, DevLog, "gg_exec");
    GunGameService.RequireDev("gg_exec");
    AdminCommand.Reply(caller, $"[GunGame] {GunGameService.Exec(caller, string.Join(" ", command))}");
  }
}
