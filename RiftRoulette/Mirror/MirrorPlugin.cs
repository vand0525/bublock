using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.GameLoop;

namespace RiftRoulette.Mirror;

public class MirrorPlugin : DeadworksPluginBase
{
  private static readonly Logger MirrorLog = BublockLog.For("Mirror");

  public override string Name => "Rift Roulette Mirror";

  [GameEventHandler("player_respawned")]
  public HookResult OnPlayerRespawned(PlayerRespawnedEvent args)
  {
    ApplyPendingNextTick(args.Userid?.As<CCitadelPlayerPawn>()?.Controller);
    return HookResult.Continue;
  }

  [GameEventHandler("player_spawn")]
  public HookResult OnPlayerSpawn(PlayerSpawnEvent args)
  {
    ApplyPendingNextTick(args.UseridController?.As<CCitadelPlayerController>());
    return HookResult.Continue;
  }

  private void ApplyPendingNextTick(CCitadelPlayerController? player)
  {
    if (player == null || player.IsBot || !MatchConfig.IsMirror || MirrorModeService.PendingCount == 0)
      return;

    var steamId = player.PlayerSteamId;

    Timer.NextTick(() =>
    {
      var current = Players.GetAll().FirstOrDefault(candidate => candidate.PlayerSteamId == steamId);

      if (current != null)
        MirrorModeService.ApplyPending(current, Timer);
    });
  }

  [Command("mirror_hero", Description = "Mirror mode: pin everyone's hero, or clear: mirror_hero <hero|clear>")]
  public void CmdMirrorHero(CCitadelPlayerController? caller, params string[] hero)
  {
    AdminCommand.Authorize(caller, MirrorLog, "mirror_hero");
    AdminCommand.Reply(caller, $"[Mirror] {MirrorModeService.PinHero(string.Join(' ', hero), Timer, ExecutionMode.Debug)}");
  }

  [Command("mirror_build", Description = "Mirror mode: pin which of the pinned hero's builds everyone gets, or clear: mirror_build <1|2|3|clear>")]
  public void CmdMirrorBuild(CCitadelPlayerController? caller, params string[] slot)
  {
    AdminCommand.Authorize(caller, MirrorLog, "mirror_build");
    AdminCommand.Reply(caller, $"[Mirror] {MirrorModeService.PinBuild(string.Join(' ', slot), Timer, ExecutionMode.Debug)}");
  }

  [Command("mirror_status", Description = "Mirror mode: show the pins, the current shared hero and build, and fighters")]
  public void CmdMirrorStatus(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, MirrorLog, "mirror_status");

    foreach (var line in MirrorModeService.Describe())
      AdminCommand.Reply(caller, $"[Mirror] {line}");
  }
}
