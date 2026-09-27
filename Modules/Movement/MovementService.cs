using System.Numerics;
using Bublock.Shared;
using DeadworksManaged.Api;

namespace Bublock.Modules.Movement;

public static class MovementService
{
  private static readonly Logger Log = BublockLog.For("Movement");

  public static LocationRegistry Locations { get; } = new();

  public static bool TeleportTo(
    CCitadelPlayerController player,
    MovementLocation location,
    ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var pawn = player.GetHeroPawn();

    if (pawn == null)
    {
      log.Debug(player.ToPlayerRef(), "Teleport skipped, no pawn Location={Location}", location.Name);
      return false;
    }

    pawn.Teleport(
      position: location.Position,
      angles: null,
      velocity: Vector3.Zero
    );

    SetViewAngle(player, location.Angle);

    log.Debug(player.ToPlayerRef(), "Teleported Location={Location} Position={Position}", location.Name, location.Position);
    return true;
  }

  public static int TeleportPlayers(
    IEnumerable<CCitadelPlayerController> players,
    MovementLocation location,
    ExecutionMode mode = ExecutionMode.Clean)
  {
    var moved = 0;

    foreach (var player in players)
    {
      if (TeleportTo(player, location, mode))
        moved++;
    }

    Log.WithMode(mode).Debug("Teleported players Location={Location} Moved={Moved}", location.Name, moved);
    return moved;
  }

  public static void SetViewAngle(CCitadelPlayerController player, Vector3 angle)
  {
    NetMessages.Send(
      new CCitadelUserMsg_SetClientCameraAngles
      {
        PlayerSlot = player.Slot,
        CameraAngles = new CMsgQAngle
        {
          X = angle.X,
          Y = angle.Y,
          Z = angle.Z
        }
      },
      RecipientFilter.Single(player.Slot)
    );
  }

  public static (Vector3 Position, Vector3 EyeAngles)? Where(CCitadelPlayerController player)
  {
    var pawn = player.GetHeroPawn();

    return pawn == null ? null : (pawn.Position, pawn.EyeAngles);
  }
}
