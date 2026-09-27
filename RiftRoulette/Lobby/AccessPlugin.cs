using Bublock.Shared;
using DeadworksManaged.Api;

namespace RiftRoulette.Lobby;

public class AccessPlugin : DeadworksPluginBase
{
  private static readonly Logger AccessLog = BublockLog.For("Access");

  public override string Name => "Rift Roulette Access";

  public override void OnLoad(bool isReload)
  {
    AccessService.Load();
  }

  [Command("player_ban", Description = "Ban a connected player by slot and kick them: player_ban <slot>")]
  public void CmdPlayerBan(CCitadelPlayerController? caller, int slot)
  {
    AdminCommand.Authorize(caller, AccessLog, "player_ban");

    var player = Players.GetAll().FirstOrDefault(candidate => candidate.Slot == slot)
      ?? throw new CommandException($"No player in slot {slot}.");

    if (player.IsBot || player.PlayerSteamId == 0)
      throw new CommandException($"{player.PlayerName} is a bot.");

    if (caller != null && caller.PlayerSteamId == player.PlayerSteamId)
      throw new CommandException("You can't ban yourself.");

    AccessLog.WithMode(ExecutionMode.Debug).Info(player.ToPlayerRef(), "Banning connected player");

    var reply = AccessService.Ban(player.PlayerSteamId, ExecutionMode.Debug);
    LobbyService.KickPlayer(slot, ExecutionMode.Debug);

    AdminCommand.Reply(caller, $"[Access] {player.PlayerName}: {reply} Kicked slot {slot}.");
  }

  [Command("ban_add", Description = "Ban a Steam64 ID: ban_add <steamid>")]
  public void CmdBanAdd(CCitadelPlayerController? caller, string steamId)
  {
    AdminCommand.Authorize(caller, AccessLog, "ban_add");

    var id = ParseSteamId(steamId);
    var reply = AccessService.Ban(id, ExecutionMode.Debug);
    var kicked = AccessService.KickDenied(ExecutionMode.Debug);

    AdminCommand.Reply(caller, $"[Access] {reply}{KickedSuffix(kicked)}");
  }

  [Command("ban_remove", Description = "Unban a Steam64 ID: ban_remove <steamid>")]
  public void CmdBanRemove(CCitadelPlayerController? caller, string steamId)
  {
    AdminCommand.Authorize(caller, AccessLog, "ban_remove");

    AdminCommand.Reply(caller, $"[Access] {AccessService.Unban(ParseSteamId(steamId), ExecutionMode.Debug)}");
  }

  [Command("ban_list", Description = "List banned Steam64 IDs")]
  public void CmdBanList(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, AccessLog, "ban_list");

    foreach (var line in AccessService.DescribeBanned())
      AdminCommand.Reply(caller, $"[Access] {line}");
  }

  [Command("allow_add", Description = "Whitelist a Steam64 ID for private mode: allow_add <steamid>")]
  public void CmdAllowAdd(CCitadelPlayerController? caller, string steamId)
  {
    AdminCommand.Authorize(caller, AccessLog, "allow_add");

    AdminCommand.Reply(caller, $"[Access] {AccessService.Allow(ParseSteamId(steamId), ExecutionMode.Debug)}");
  }

  [Command("allow_remove", Description = "Remove a Steam64 ID from the whitelist: allow_remove <steamid>")]
  public void CmdAllowRemove(CCitadelPlayerController? caller, string steamId)
  {
    AdminCommand.Authorize(caller, AccessLog, "allow_remove");

    AdminCommand.Reply(caller, $"[Access] {AccessService.Disallow(ParseSteamId(steamId), ExecutionMode.Debug)}");
  }

  [Command("allow_list", Description = "List whitelisted Steam64 IDs")]
  public void CmdAllowList(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, AccessLog, "allow_list");

    foreach (var line in AccessService.DescribeAllowed())
      AdminCommand.Reply(caller, $"[Access] {line}");
  }

  [Command("access_mode", Description = "Show join access, or set it: access_mode [open|private]")]
  public void CmdAccessMode(CCitadelPlayerController? caller, string mode = "")
  {
    AdminCommand.Authorize(caller, AccessLog, "access_mode");

    switch (mode.Trim().ToLowerInvariant())
    {
      case "":
        foreach (var line in AccessService.Describe())
          AdminCommand.Reply(caller, $"[Access] {line}");
        return;

      case "open":
        AdminCommand.Reply(caller, $"[Access] {AccessService.SetPrivate(false, ExecutionMode.Debug)}");
        return;

      case "private":
        var reply = AccessService.SetPrivate(true, ExecutionMode.Debug);
        var kicked = AccessService.KickDenied(ExecutionMode.Debug);
        AdminCommand.Reply(caller, $"[Access] {reply}{KickedSuffix(kicked)}");
        return;

      default:
        throw new CommandException("Mode must be open or private.");
    }
  }

  private static ulong ParseSteamId(string text) =>
    AccessRule.TryParseSteamId(text, out var steamId)
      ? steamId
      : throw new CommandException($"'{text}' is not a Steam64 ID (17 digits, starts with 7656119).");

  private static string KickedSuffix(IReadOnlyList<string> kicked) =>
    kicked.Count == 0 ? "" : $" Kicked {kicked.Count}: {string.Join(", ", kicked)}.";
}
