using Bublock.Shared;
using DeadworksManaged.Api;

namespace Bublock.Modules.Hud;

public static class HudService
{
  public const string AdminSayLabel = "Server admin";

  private static readonly Logger Log = BublockLog.For("Hud");

  public static void Announce(
    CCitadelPlayerController player,
    string title,
    string description = "",
    ExecutionMode mode = ExecutionMode.Clean)
  {
    player.HudAnnounce(title, description);

    Log.WithMode(mode).Debug(player.ToPlayerRef(), "Announced Title={Title} Description={Description}", title, description);
  }

  public static int AnnounceAll(string title, string description = "", ExecutionMode mode = ExecutionMode.Clean)
  {
    var sent = 0;

    foreach (var player in Players.GetAll())
    {
      player.HudAnnounce(title, description);
      sent++;
    }

    Log.WithMode(mode).Info("Announced to all Title={Title} Description={Description} Players={Players}", title, description, sent);
    return sent;
  }

  public static (string Title, string Description) ParseAnnouncement(string text)
  {
    var split = text.IndexOf('|');

    return split < 0
      ? (text.Trim(), "")
      : (text[..split].Trim(), text[(split + 1)..].Trim());
  }
}
