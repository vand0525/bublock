using Bublock.Shared;
using DeadworksManaged.Api;

namespace RiftRoulette.GameLoop;

public static class ShopAccess
{
  public const string ConVarName = "citadel_allow_purchasing_anywhere";

  private static readonly Logger Log = BublockLog.For("Match");

  private static bool? _applied;

  public static bool BuyAnywhere => ShopRule.BuyAnywhere(MatchConfig.IsDuel, MatchService.State.IsRunning);

  public static int? ConVarValue => ConVar.Find(ConVarName)?.GetInt();

  public static bool Sync(ExecutionMode mode = ExecutionMode.Clean)
  {
    var on = BuyAnywhere;

    ServerConVars.TrySet(ConVarName, on ? 1 : 0, Log);

    if (_applied == on)
      return false;

    _applied = on;
    Log.WithMode(mode).Info("Buying anywhere {State} Mode={Mode} MatchRunning={MatchRunning}", on ? "On" : "Off", MatchConfig.HeroMode, MatchService.State.IsRunning);
    return true;
  }
}
