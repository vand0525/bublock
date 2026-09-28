using Bublock.Modules.Session;
using Bublock.Shared;
using DeadworksManaged.Api;

namespace GunGame;

public class LobbyPlugin : DeadworksPluginBase
{
  public const int WaitingReminderSeconds = 30;

  public override string Name => "Gun Game Lobby";

  public override void OnLoad(bool isReload)
  {
    BublockLog.Master.Info("Gun Game loaded Reload={Reload}", isReload);
    GunGameService.ApplyServerConvars();

    // Again once other plugins (CleanSlate) have set their startup convars; then pick up anyone already here.
    Timer.Once(GunGameService.ConvarDelaySeconds.Seconds(), () =>
    {
      GunGameService.ApplyServerConvars();
      GunGameService.AdmitConnected(Timer);
      GunGameService.Session.Check(Timer);
    });

    Timer.Every(WaitingReminderSeconds.Seconds(), () =>
    {
      if (GunGameService.Mode != Bublock.Modules.DevMode.RunMode.Prod || GunGameService.Session.Phase != SessionPhase.Waiting)
        return;

      var humans = GunGameService.Humans();

      foreach (var player in humans)
        PlayerChat.Send(player, GunGameRules.WaitingLine(humans.Count, GunGameService.Session.Options.MinPlayers));
    });
  }

  public override void OnStartupServer()
  {
    GunGameService.ApplyServerConvars();
    Timer.Once(GunGameService.ConvarDelaySeconds.Seconds(), () => GunGameService.ApplyServerConvars());
  }

  public override void OnClientFullConnect(ClientFullConnectEvent args)
  {
    var player = args.Controller;

    if (player != null && !player.IsBot)
      GunGameService.Admit(player, Timer);
  }

  public override void OnClientDisconnect(ClientDisconnectedEvent args)
  {
    var player = args.Controller;

    if (player != null && !player.IsBot)
      GunGameService.Remove(player, Timer);
  }

  // The game decides heroes and teams: menu hero picks and team changes are refused.
  public override HookResult OnClientConCommand(ClientConCommandEvent args) =>
    GunGameService.BlocksCommand(args.Controller, args.Command) ? HookResult.Stop : HookResult.Continue;

  // Power comes only from the build a kill gives.
  public override HookResult OnModifyCurrency(ModifyCurrencyEvent args) =>
    GunGameService.BlocksCurrency(args.CurrencyType, args.Source, args.Amount) ? HookResult.Stop : HookResult.Continue;
}
