using Bublock.Modules.Movement;
using Bublock.Modules.Restraint;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Draft;
using RiftRoulette.Lobby;
using RiftRoulette.Locations;
using RiftRoulette.Rift;

namespace RiftRoulette.Round;

public static class WatchSpot
{
  private static readonly Logger Log = BublockLog.For("Round");

  private static RiftSide? _boardSide;

  public static RiftSide Side =>
    WatchSpotRule.SideFor(RiftService.IsRunning, RiftService.CurrentSide, RiftService.NextSide);

  public static RiftSide BoardSide => _boardSide ??= Side;

  public static MovementLocation Location(RiftSide side) => RoundLocations.WatchFor(side);

  public static bool SendUp(
    CCitadelPlayerController player,
    ExecutionMode mode = ExecutionMode.Clean,
    RiftSide? side = null)
  {
    RestraintService.Restrain(player, mode);
    var moved = MovementService.TeleportTo(player, SlotSpots.Watch(Location(side ?? Side), player.Slot), mode);

    WatchGuard.Grace(player.PlayerSteamId);
    return moved;
  }

  public static int MoveAllUp(ExecutionMode mode = ExecutionMode.Clean)
  {
    if (RiftService.IsRunning)
      return 0;

    var side = Side;
    var moved = 0;

    foreach (var player in Participants.Humans())
    {
      var pawn = player.GetHeroPawn();

      if (pawn == null || !pawn.IsAlive)
        continue;

      if (SendUp(player, mode, side))
        moved++;
    }

    RefreshBoards(side, mode);
    Log.WithMode(mode).Info("Players moved to the watch spot Side={Side} Moved={Moved}", RiftSides.Name(side), moved);
    return moved;
  }

  public static bool RefreshBoards(RiftSide side, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (BoardSide == side)
      return false;

    _boardSide = side;
    DraftService.RedrawBoards(mode);
    Log.WithMode(mode).Debug("Boards moved to the watch spot Side={Side}", RiftSides.Name(side));
    return true;
  }
}
