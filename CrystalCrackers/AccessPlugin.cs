using Bublock.Modules.Access;
using Bublock.Shared;
using DeadworksManaged.Api;

namespace CrystalCrackers;

public class AccessPlugin : DeadworksPluginBase
{
  private static readonly Logger AccessLog = BublockLog.For("Access");

  public override string Name => "Crystal Crackers Access";

  public override void OnLoad(bool isReload)
  {
    AccessStore.Load();
    BublockLog.Master.Info("Access gate on, always private Reload={Reload}", isReload);

    Timer.Once(AccessGate.LoadKickSeconds.Seconds(), () =>
    {
      var kicked = AccessGate.KickWithoutAccess();

      if (kicked.Count > 0)
        BublockLog.Master.Info("Kicked players without access Count={Count}", kicked.Count);
    });
  }

  public override bool OnClientConnect(ClientConnectEvent args) =>
    AccessGate.AllowConnect(args.SteamId, args.Name);

  [Command("access_mode", Description = "Show join access (Crystal Crackers is always private)")]
  public void CmdAccessMode(CCitadelPlayerController? caller, string mode = "")
  {
    AdminCommand.Authorize(caller, AccessLog, "access_mode");

    if (mode.Trim().Length > 0)
      throw new CommandException("Crystal Crackers is always private; the mode can't be changed here.");

    AdminCommand.Reply(caller, "[Access] Crystal Crackers is always private (only admins and the allowed list); the file's mode is not used.");

    foreach (var line in AccessStore.Describe())
      AdminCommand.Reply(caller, $"[Access] {line}");
  }

  [Command("ban_add", Description = "Ban a Steam64 ID; kicked at once if connected: ban_add <steamid>")]
  public void CmdBanAdd(CCitadelPlayerController? caller, string steamId)
  {
    AdminCommand.Authorize(caller, AccessLog, "ban_add");

    var reply = AccessStore.Ban(ParseSteamId(steamId), ExecutionMode.Debug);
    AdminCommand.Reply(caller, $"[Access] {reply}{KickedSuffix(AccessGate.KickWithoutAccess(ExecutionMode.Debug))}");
  }

  [Command("ban_remove", Description = "Unban a Steam64 ID: ban_remove <steamid>")]
  public void CmdBanRemove(CCitadelPlayerController? caller, string steamId)
  {
    AdminCommand.Authorize(caller, AccessLog, "ban_remove");

    AdminCommand.Reply(caller, $"[Access] {AccessStore.Unban(ParseSteamId(steamId), ExecutionMode.Debug)}");
  }

  [Command("ban_list", Description = "List banned Steam64 IDs")]
  public void CmdBanList(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, AccessLog, "ban_list");

    foreach (var line in AccessStore.DescribeIds("Banned", AccessStore.Load().Banned))
      AdminCommand.Reply(caller, $"[Access] {line}");
  }

  [Command("allow_add", Description = "Let a Steam64 ID join: allow_add <steamid>")]
  public void CmdAllowAdd(CCitadelPlayerController? caller, string steamId)
  {
    AdminCommand.Authorize(caller, AccessLog, "allow_add");

    AdminCommand.Reply(caller, $"[Access] {AccessStore.Allow(ParseSteamId(steamId), ExecutionMode.Debug)}");
  }

  [Command("allow_remove", Description = "Take a Steam64 ID off the allowed list; kicked at once if connected: allow_remove <steamid>")]
  public void CmdAllowRemove(CCitadelPlayerController? caller, string steamId)
  {
    AdminCommand.Authorize(caller, AccessLog, "allow_remove");

    var reply = AccessStore.Disallow(ParseSteamId(steamId), ExecutionMode.Debug);
    AdminCommand.Reply(caller, $"[Access] {reply}{KickedSuffix(AccessGate.KickWithoutAccess(ExecutionMode.Debug))}");
  }

  [Command("allow_list", Description = "List Steam64 IDs allowed to join")]
  public void CmdAllowList(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, AccessLog, "allow_list");

    foreach (var line in AccessStore.DescribeIds("Allowed", AccessStore.Load().Allowed))
      AdminCommand.Reply(caller, $"[Access] {line}");
  }

  private static ulong ParseSteamId(string text) =>
    AccessRule.TryParseSteamId(text, out var steamId)
      ? steamId
      : throw new CommandException($"'{text}' is not a Steam64 ID (17 digits, starts with 7656119).");

  private static string KickedSuffix(IReadOnlyList<string> kicked) =>
    kicked.Count == 0 ? "" : $" Kicked {kicked.Count}: {string.Join(", ", kicked)}.";
}
