using DeadworksManaged.Api;

namespace Bublock.Shared;

public static class AdminCommand
{
  public const string RejectedMessage = "You are not allowed to use this command.";

  public static void Authorize(CCitadelPlayerController? caller, Logger log, string command)
  {
    if (caller == null)
    {
      log.Info("Admin command {Command} from server console", command);
      return;
    }

    if (!AdminAuth.IsAuthorized(caller.PlayerSteamId))
    {
      log.Warn(caller.ToPlayerRef(), "Unauthorized command attempted {Command}", command);
      throw new CommandException(RejectedMessage);
    }

    log.Info(caller.ToPlayerRef(), "Admin command {Command}", command);
  }

  public static void Reply(CCitadelPlayerController? caller, string message)
  {
    if (caller == null)
      Console.WriteLine(message);
    else
      caller.PrintToConsole(message);
  }
}
