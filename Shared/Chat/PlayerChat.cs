using DeadworksManaged.Api;

namespace Bublock.Shared;

public static class PlayerChat
{
  public static void Send(CCitadelPlayerController player, string message)
  {
    NetMessages.Send(
        new CCitadelUserMsg_ChatMsg
        {
          PlayerSlot = player.Slot,
          Text = message,
          AllChat = true
        },
        RecipientFilter.Single(player.Slot)
    );
  }
}
