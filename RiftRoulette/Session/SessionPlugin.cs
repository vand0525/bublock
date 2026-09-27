using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Locations;

namespace RiftRoulette.Session;

public class SessionPlugin : DeadworksPluginBase
{
  private static readonly Logger SessionLog = BublockLog.For("Session");

  public override string Name => "Rift Roulette Session";

  public override void OnLoad(bool isReload)
  {
    BublockLog.Master.Info(
      "Session started Reload={Reload} LogFolder={LogFolder}",
      isReload,
      BublockLog.Directory
    );

    RiftRouletteLocations.RegisterAll();
  }

  public override void OnStartupServer()
  {
    BublockLog.Master.Info("Server startup Map={Map}", Server.MapName);
  }

  public override void OnUnload()
  {
    BublockLog.Master.Info("Session ending (plugin unload)");
  }

  [Command("session_info", Description = "Show the Rift Roulette session id, round, map, and log folder")]
  public void CmdSessionInfo(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, SessionLog, "session_info");

    AdminCommand.Reply(caller, $"[Session] Session={BublockLog.SessionId}");
    AdminCommand.Reply(caller, $"[Session] Round={BublockLog.RoundId ?? "-"}");
    AdminCommand.Reply(caller, $"[Session] Map={Server.MapName}");
    AdminCommand.Reply(caller, $"[Session] LogFolder={BublockLog.Directory}");
    AdminCommand.Reply(caller, $"[Session] FileLogging={(BublockLog.Hub.FileLoggingEnabled ? "on" : "OFF")}");
  }
}
