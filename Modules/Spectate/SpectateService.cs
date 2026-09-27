using System.Numerics;
using Bublock.Modules.Movement;
using Bublock.Shared;
using DeadworksManaged.Api;

namespace Bublock.Modules.Spectate;

public static class SpectateService
{
  public const string ObserverDesignerName = "observer";

  private static readonly Logger Log = BublockLog.For("Spectate");

  public static CBasePlayerPawn? Observer(CCitadelPlayerController player) =>
    player.Pawn is { } pawn && pawn.DesignerName == ObserverDesignerName ? pawn : null;

  public static bool IsObserving(CCitadelPlayerController player) => Observer(player) != null;

  public static CBaseEntity? Current(CCitadelPlayerController player) => Observer(player)?.ObserverTarget;

  public static ObserverMode_t Mode(CCitadelPlayerController player) =>
    Observer(player)?.ObserverMode ?? ObserverMode_t.None;

  public static bool IsWatching(CCitadelPlayerController player, CBaseEntity? target) =>
    target != null && Current(player) is { } current && current.EntityHandle == target.EntityHandle;

  // Never IsValidObserverTarget: it rejects TeamNum 3, a playing team in Deadlock.
  public static bool Follow(CCitadelPlayerController player, CCitadelPlayerController target, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var observer = Observer(player);
    var pawn = target.GetHeroPawn();

    if (observer == null || pawn == null)
    {
      log.Debug(player.ToPlayerRef(), "Follow skipped Observer={Observer} TargetPawn={TargetPawn}", observer != null, pawn != null);
      return false;
    }

    observer.SetObserverMode(ObserverMode_t.InEye);

    if (observer.SetObserverTarget(pawn))
    {
      log.Debug(player.ToPlayerRef(), "Following Target={Target} TargetSlot={TargetSlot}", target.PlayerName, target.Slot);
      return true;
    }

    ClientFollow(player, target, mode);
    return false;
  }

  public static void ClientFollow(CCitadelPlayerController player, CCitadelPlayerController target, ExecutionMode mode = ExecutionMode.Clean)
  {
    Server.ClientCommand(player.Slot, $"spec_player {target.Slot}");
    Log.WithMode(mode).Info(player.ToPlayerRef(), "Server target refused, sent client command Command={Command}", $"spec_player {target.Slot}");
  }

  public static bool Park(CCitadelPlayerController player, Vector3 position, Vector3 angle, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var observer = Observer(player);

    if (observer == null)
    {
      log.Debug(player.ToPlayerRef(), "Park skipped, not an observer");
      return false;
    }

    observer.SetObserverMode(ObserverMode_t.Roaming);
    observer.Teleport(position: position, angles: angle, velocity: Vector3.Zero);
    MovementService.SetViewAngle(player, angle);

    log.Debug(player.ToPlayerRef(), "Parked Position={Position} Angle={Angle} After={After}", position, angle, observer.Position);
    return true;
  }
}
