using System.Text.Json;
using Bublock.Shared;
using DeadworksManaged.Api;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.Lobby;

public static class AccessService
{
  public const string FileName = "access.json";

  private static readonly Logger Log = BublockLog.For("Access");

  private static readonly object Gate = new();

  private static AccessList _current = new();

  private static DateTime? _loadedWriteTime;

  public static string FilePath =>
    Path.Combine(Path.GetDirectoryName(LogPaths.ResolveRoot())!, FileName);

  public static AccessList Load()
  {
    lock (Gate)
    {
      var path = FilePath;

      try
      {
        if (!File.Exists(path))
        {
          if (_loadedWriteTime != null)
            Log.Info("Access file missing, server is open with empty lists Path={Path}", path);

          _current = new AccessList();
          _loadedWriteTime = null;
          return _current;
        }

        var writeTime = File.GetLastWriteTimeUtc(path);

        if (writeTime == _loadedWriteTime)
          return _current;

        _current = AccessList.Parse(File.ReadAllText(path));
        _loadedWriteTime = writeTime;

        Log.Info(
          "Access file loaded Private={Private} Banned={Banned} Allowed={Allowed}",
          _current.Private,
          _current.Banned.Count,
          _current.Allowed.Count);
      }
      catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
      {
        Log.Error(exception, "Access file unreadable, keeping the last good lists Path={Path}", path);
      }

      return _current;
    }
  }

  public static bool AllowConnect(ulong steamId, string name)
  {
    if (steamId == 0)
      return true;

    var verdict = Load().Check(steamId, AdminAuth.IsAuthorized(steamId));

    switch (verdict)
    {
      case AccessVerdict.Banned:
        return BanStatueService.AdmitBanned(steamId, name);

      case AccessVerdict.Private:
        Log.Warn("Connection refused, server is private Name={Name} SteamId={SteamId}", name, steamId);
        return false;

      default:
        return true;
    }
  }

  public static AccessVerdict Check(CCitadelPlayerController player) =>
    Load().Check(player.PlayerSteamId, AdminAuth.IsAuthorized(player.PlayerSteamId));

  public static string Ban(ulong steamId, ExecutionMode mode = ExecutionMode.Clean) =>
    Change(list => list.Banned.Add(steamId), mode, "Banned", steamId, $"{steamId} is banned.", $"{steamId} was already banned.");

  public static string Unban(ulong steamId, ExecutionMode mode = ExecutionMode.Clean) =>
    Change(list => list.Banned.Remove(steamId), mode, "Unbanned", steamId, $"{steamId} is no longer banned.", $"{steamId} was not banned.");

  public static string Allow(ulong steamId, ExecutionMode mode = ExecutionMode.Clean) =>
    Change(list => list.Allowed.Add(steamId), mode, "Whitelisted", steamId, $"{steamId} is whitelisted.", $"{steamId} was already whitelisted.");

  public static string Disallow(ulong steamId, ExecutionMode mode = ExecutionMode.Clean) =>
    Change(list => list.Allowed.Remove(steamId), mode, "Removed from whitelist", steamId, $"{steamId} is off the whitelist.", $"{steamId} was not whitelisted.");

  public static string SetPrivate(bool privateMode, ExecutionMode mode = ExecutionMode.Clean)
  {
    var name = privateMode ? "private" : "open";

    return Change(
      list =>
      {
        var changed = list.Private != privateMode;
        list.Private = privateMode;
        return changed;
      },
      mode,
      "Access mode set",
      name,
      $"Server is {name}.",
      $"Server was already {name}.");
  }

  public static string SetStatueModifier(string? name, ExecutionMode mode = ExecutionMode.Clean)
  {
    var modifier = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    var shown = modifier ?? "none";

    return Change(
      list =>
      {
        var changed = list.StatueModifier != modifier;
        list.StatueModifier = modifier;
        return changed;
      },
      mode,
      "Statue modifier set",
      shown,
      $"Statue modifier is {shown}.",
      $"Statue modifier was already {shown}.");
  }

  // Banned players still in the game become statues and are kicked after LiveBanKickSeconds.
  public static IReadOnlyList<string> PetrifyBanned(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var list = Load();
    var petrified = new List<string>();

    foreach (var player in Players.GetAll().Where(player => !player.IsBot && list.Banned.Contains(player.PlayerSteamId)).ToList())
    {
      if (BanStatueService.IsStatue(player.PlayerSteamId))
        continue;

      BanStatueService.Petrify(player, BanStatueService.LiveBanKickSeconds, liveBan: true, timer, mode);
      petrified.Add(player.PlayerName);
    }

    return petrified;
  }

  // Banned players are left to PetrifyBanned; this kicks only players shut out by private mode.
  public static IReadOnlyList<string> KickDenied(ExecutionMode mode = ExecutionMode.Clean)
  {
    var list = Load();
    var kicked = new List<string>();

    foreach (var player in Players.GetAll().Where(player => !player.IsBot && player.PlayerSteamId != 0).ToList())
    {
      var verdict = list.Check(player.PlayerSteamId, AdminAuth.IsAuthorized(player.PlayerSteamId));

      if (verdict != AccessVerdict.Private)
        continue;

      Log.WithMode(mode).Info(player.ToPlayerRef(), "Kicking player without access Verdict={Verdict}", verdict);

      if (LobbyService.KickPlayer(player.Slot, mode))
        kicked.Add(player.PlayerName);
    }

    return kicked;
  }

  public static IReadOnlyList<string> Describe()
  {
    var list = Load();

    return
    [
      $"Mode={(list.Private ? "private" : "open")} | StatueModifier={list.StatueModifier ?? "none"} | File={FilePath}",
      $"Banned ({list.Banned.Count}): {Join(list.Banned)}",
      $"Allowed ({list.Allowed.Count}): {Join(list.Allowed)}"
    ];
  }

  public static IReadOnlyList<string> DescribeBanned() =>
    DescribeIds("Banned", Load().Banned, steamId => $" | {BanStatueService.Describe(steamId)}");

  public static IReadOnlyList<string> DescribeAllowed() => DescribeIds("Allowed", Load().Allowed, _ => "");

  private static string Change(
    Func<AccessList, bool> change,
    ExecutionMode mode,
    string action,
    object subject,
    string changedReply,
    string unchangedReply)
  {
    lock (Gate)
    {
      var list = Load();

      if (!change(list))
        return unchangedReply;

      Save(list);

      Log.WithMode(mode).Info("{Action} Subject={Subject}", action, subject);
      BublockLog.Master.Info("Access changed {Action} Subject={Subject}", action, subject);
      return changedReply;
    }
  }

  private static void Save(AccessList list)
  {
    var path = FilePath;
    var temp = path + ".tmp";

    try
    {
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      File.WriteAllText(temp, list.ToJson());
      File.Move(temp, path, overwrite: true);

      _current = list;
      _loadedWriteTime = File.GetLastWriteTimeUtc(path);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      Log.Error(exception, "Access file write failed Path={Path}", path);
      _loadedWriteTime = null;
      throw new CommandException($"Could not write {path}: {exception.Message}");
    }
  }

  private static IReadOnlyList<string> DescribeIds(string title, IReadOnlyCollection<ulong> ids, Func<ulong, string> suffix)
  {
    var connected = Players.GetAll()
      .Where(player => !player.IsBot)
      .GroupBy(player => player.PlayerSteamId)
      .ToDictionary(group => group.Key, group => group.First().PlayerName);

    var lines = new List<string> { $"{title} ({ids.Count})" };

    foreach (var steamId in ids)
      lines.Add((connected.TryGetValue(steamId, out var name) ? $"{steamId} ({name}, connected)" : steamId.ToString()) + suffix(steamId));

    return lines;
  }

  private static string Join(IReadOnlyCollection<ulong> ids) =>
    ids.Count == 0 ? "none" : string.Join(", ", ids);
}
