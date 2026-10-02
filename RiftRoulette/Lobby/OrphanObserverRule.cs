namespace RiftRoulette.Lobby;

public static class OrphanObserverRule
{
  public const uint NoHandle = uint.MaxValue;

  // Same mask as CBaseEntity.EntityIndex; a controller's entity index is its slot + 1.
  public const uint IndexMask = 0x3FFF;

  public static int SlotOf(uint controller) =>
    controller == NoHandle ? -1 : (int)(controller & IndexMask) - 1;

  // Every connected player owns an observer pawn (a hero player keeps a spare one), so only the leaver's go.
  public static bool ShouldRemove(uint owner, int leaverSlot, uint leaverController, uint slotController, bool slotConnected)
  {
    if (owner == NoHandle || slotConnected || SlotOf(owner) != leaverSlot)
      return false;

    return leaverController != NoHandle ? owner == leaverController : owner != slotController;
  }
}
