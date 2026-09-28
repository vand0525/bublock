using Bublock.Modules.Arena;
using Bublock.Shared;
using DeadworksManaged.Api;

namespace GunGame;

public class ArenaPlugin : DeadworksPluginBase
{
  public const double ContainSeconds = 1.0;

  private static readonly Logger ArenaLog = BublockLog.For("Arena");

  public override string Name => "Gun Game Arena";

  public override void OnLoad(bool isReload) =>
    Timer.Every(ContainSeconds.Seconds(), () => GunGameService.ContainPlayers());

  [GameEventHandler("player_spawn")]
  public HookResult OnPlayerSpawn(PlayerSpawnEvent args)
  {
    if (args.UseridController?.As<CCitadelPlayerController>() is { } player)
      GunGameService.OnSpawn(player, Timer);

    return HookResult.Continue;
  }

  [GameEventHandler("player_respawned")]
  public HookResult OnPlayerRespawned(PlayerRespawnedEvent args)
  {
    if (args.Userid?.As<CCitadelPlayerPawn>()?.Controller is { } player)
      GunGameService.OnSpawn(player, Timer);

    return HookResult.Continue;
  }

  [Command("gg_arena", Description = "Send a player to their arena spot now: gg_arena <slot>")]
  public void CmdArena(CCitadelPlayerController? caller, int slot)
  {
    AdminCommand.Authorize(caller, ArenaLog, "gg_arena");

    var player = Players.GetAll().FirstOrDefault(candidate => candidate.Slot == slot)
      ?? throw new CommandException($"No player in slot {slot}.");

    AdminCommand.Reply(caller, ArenaService.SendToArena(player, GunGameService.Arena, ExecutionMode.Debug)
      ? $"[Arena] {player.PlayerName} sent to {GunGameService.Arena.Name}."
      : $"[Arena] {player.PlayerName} has no hero or no team.");
  }
}
