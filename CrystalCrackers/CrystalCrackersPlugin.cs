using Bublock.Shared;
using DeadworksManaged.Api;

namespace CrystalCrackers;

public class CrystalCrackersPlugin : DeadworksPluginBase
{
  public override string Name => "Crystal Crackers";

  public override void OnLoad(bool isReload)
  {
    BublockLog.Master.Info(
      "Module host started Reload={Reload} LogFolder={LogFolder}",
      isReload,
      BublockLog.Directory);
  }

  public override void OnStartupServer()
  {
    BublockLog.Master.Info("Server startup Map={Map}", Server.MapName);
  }

  public override void OnUnload()
  {
    BublockLog.Master.Info("Module host stopping (plugin unload)");
  }
}
