using DeadworksManaged.Api;

namespace __NAME__;

public class ArenaPlugin : DeadworksPluginBase
{
  public const double ContainSeconds = 1.0;

  public override string Name => "__TITLE__ Arena";

  public override void OnLoad(bool isReload) =>
    Timer.Every(ContainSeconds.Seconds(), () => __NAME__Service.ContainPlayers());

  [GameEventHandler("player_spawn")]
  public HookResult OnPlayerSpawn(PlayerSpawnEvent args)
  {
    if (args.UseridController?.As<CCitadelPlayerController>() is { } player)
      __NAME__Service.OnSpawn(player, Timer);

    return HookResult.Continue;
  }
}
