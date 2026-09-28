using Bublock.Modules.Movement;
using Bublock.Shared;
using DeadworksManaged.Api;
using ITimer = DeadworksManaged.Api.ITimer;

namespace Bublock.Modules.Arena;

public static class ArenaService
{
  private static readonly Logger Log = BublockLog.For("Arena");

  public static bool SendToArena(CCitadelPlayerController player, ArenaSpots arena, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (arena.For(player.TeamNum, player.Slot) is not { } spot)
    {
      Log.WithMode(mode).Debug(player.ToPlayerRef(), "Arena skipped, no playable team TeamNum={TeamNum}", player.TeamNum);
      return false;
    }

    var sent = MovementService.TeleportTo(player, spot, mode);
    Log.WithMode(mode).Debug(player.ToPlayerRef(), "Arena spawn Arena={Arena} Spot={Spot} Sent={Sent}", arena.Name, spot.Name, sent);
    return sent;
  }

  // Containment: every living player outside the arena's bounds goes back to their own spot.
  public static int ReturnStrays(IEnumerable<CCitadelPlayerController> players, ArenaSpots arena, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (arena.Bounds == null)
      return 0;

    var returned = 0;

    foreach (var player in players)
    {
      var pawn = player.GetHeroPawn();

      if (pawn == null || !pawn.IsAlive || arena.Contains(pawn.Position))
        continue;

      Log.WithMode(mode).Info(player.ToPlayerRef(), "Left the arena, sent back Arena={Arena} Position={Position}", arena.Name, pawn.Position);

      if (SendToArena(player, arena, mode))
        returned++;
    }

    return returned;
  }

  // From a spawn hook: the teleport runs on the next tick, outside the event.
  public static void SendToArenaNextTick(CCitadelPlayerController player, ArenaSpots arena, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;
    var slot = player.Slot;

    timer.NextTick(() =>
    {
      var current = Players.GetAll().FirstOrDefault(candidate => candidate.Slot == slot && candidate.PlayerSteamId == steamId);

      if (current != null)
        SendToArena(current, arena, mode);
    });
  }
}
