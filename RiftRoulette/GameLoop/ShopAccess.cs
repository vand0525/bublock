using Bublock.Shared;
using DeadworksManaged.Api;

namespace RiftRoulette.GameLoop;

public static class ShopAccess
{
  public const string ConVarName = "citadel_allow_purchasing_anywhere";

  private static readonly Logger Log = BublockLog.For("Match");

  public static int? ConVarValue => ConVar.Find(ConVarName)?.GetInt();

  public static void Disable(ExecutionMode mode = ExecutionMode.Clean)
  {
    ServerConVars.TrySet(ConVarName, 0, Log);
    Log.WithMode(mode).Debug("Buying anywhere off ConVar={ConVar}", ConVarValue?.ToString() ?? "missing");
  }
}
