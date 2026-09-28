using Bublock.Shared;
using DeadworksManaged.Api;

namespace __NAME__;

public class LobbyPlugin : DeadworksPluginBase
{
  public override string Name => "__TITLE__ Lobby";

  public override void OnLoad(bool isReload)
  {
    BublockLog.Master.Info("__TITLE__ loaded Reload={Reload}", isReload);
    __NAME__Service.ApplyServerConvars();

    Timer.Once(__NAME__Service.ConvarDelaySeconds.Seconds(), () =>
    {
      __NAME__Service.ApplyServerConvars();
      __NAME__Service.Session.Check(Timer);
    });
  }

  public override void OnStartupServer() => __NAME__Service.ApplyServerConvars();

  public override void OnClientFullConnect(ClientFullConnectEvent args)
  {
    if (args.Controller is { IsBot: false } player)
      __NAME__Service.Admit(player, Timer);
  }

  public override void OnClientDisconnect(ClientDisconnectedEvent args)
  {
    if (args.Controller is { IsBot: false } player)
      __NAME__Service.Remove(player, Timer);
  }

  public override HookResult OnClientConCommand(ClientConCommandEvent args) =>
    __NAME__Service.BlocksCommand(args.Controller, args.Command) ? HookResult.Stop : HookResult.Continue;

  public override HookResult OnModifyCurrency(ModifyCurrencyEvent args) =>
    __NAME__Service.BlocksCurrency(args.CurrencyType, args.Source, args.Amount) ? HookResult.Stop : HookResult.Continue;
}
