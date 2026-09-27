using Bublock.Shared;
using DeadworksManaged.Api;

namespace Bublock.Modules.Hud;

public class HudPlugin : DeadworksPluginBase
{
  private static readonly Logger CommandsLog = BublockLog.For("Hud");

  public override string Name => "Hud";

  [Command("hud_announce", Description = "Show an on-screen banner to everyone: hud_announce <title> [| description]")]
  public void CmdAnnounce(CCitadelPlayerController? caller, params string[] text)
  {
    AdminCommand.Authorize(caller, CommandsLog, "hud_announce");

    var (title, description) = HudService.ParseAnnouncement(string.Join(' ', text));

    if (title.Length == 0)
      throw new CommandException("Usage: hud_announce <title> [| description]");

    var sent = HudService.AnnounceAll(title, description, ExecutionMode.Debug);
    AdminCommand.Reply(caller, $"[Hud] Announced to {sent} player(s)");
  }

  [Command("hud_say", Description = "Say something to everyone as a big on-screen banner: hud_say <message>")]
  public void CmdSay(CCitadelPlayerController? caller, params string[] text)
  {
    AdminCommand.Authorize(caller, CommandsLog, "hud_say");

    var message = string.Join(' ', text).Trim();

    if (message.Length == 0)
      throw new CommandException("Usage: hud_say <message>");

    var sent = HudService.AnnounceAll(message, HudService.AdminSayLabel, ExecutionMode.Debug);
    AdminCommand.Reply(caller, $"[Hud] Said to {sent} player(s): {message}");
  }
}
