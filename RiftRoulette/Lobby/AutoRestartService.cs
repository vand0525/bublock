using Bublock.Shared;
using DeadworksManaged.Api;

namespace RiftRoulette.Lobby;

public static class AutoRestartService
{
  public const int CheckSeconds = 60;

  private static readonly Logger Log = BublockLog.For("Restart");

  private static readonly Dictionary<int, PendingJoin> Pending = [];

  private sealed record PendingJoin(ulong SteamId, string Name, DateTime Since)
  {
    public bool Stuck { get; set; }
  }

  public static bool Enabled { get; private set; } = true;

  public static int StuckJoins { get; private set; }

  private static DateTime? _mapStartedAt;

  private static DateTime? _lastReloadAt;

  // After a hot reload the map start is unknown: fall back to the engine clock (GlobalVars.CurTime).
  // A reload we asked for also counts as a start, in case OnStartupServer does not run on changelevel.
  public static double UptimeSeconds
  {
    get
    {
      var since = new[] { _mapStartedAt, _lastReloadAt }.Max();
      return since is { } start ? (DateTime.UtcNow - start).TotalSeconds : GlobalVars.CurTime;
    }
  }

  public static void OnConnect(ClientConnectEvent args, bool allowed)
  {
    if (!allowed)
      return;

    Pending[args.Slot] = new PendingJoin(args.SteamId, args.Name, DateTime.UtcNow);
    Log.Info(
      "Client connecting Name={Name} SteamId={SteamId} Slot={Slot} MapChangeReconnect={MapChangeReconnect}",
      args.Name, args.SteamId, args.Slot, args.IsMapChangeReconnect);
  }

  public static void OnFullConnect(int slot, CCitadelPlayerController? player)
  {
    if (!Pending.Remove(slot, out var join))
      return;

    var seconds = (DateTime.UtcNow - join.Since).TotalSeconds;

    if (player != null)
      Log.Info(player.ToPlayerRef(), "Join completed Seconds={Seconds}", Math.Round(seconds, 1));
    else
      Log.Info("Join completed Name={Name} SteamId={SteamId} Seconds={Seconds}", join.Name, join.SteamId, Math.Round(seconds, 1));
  }

  public static void OnDisconnect(int slot, bool mapChange)
  {
    if (!Pending.Remove(slot, out var join) || mapChange)
      return;

    var seconds = (DateTime.UtcNow - join.Since).TotalSeconds;

    if (!join.Stuck)
      StuckJoins++;

    Log.Warn(
      "Join never completed, client left Name={Name} SteamId={SteamId} Slot={Slot} Seconds={Seconds} StuckJoins={StuckJoins}",
      join.Name, join.SteamId, slot, Math.Round(seconds, 1), StuckJoins);
  }

  public static void OnMapStart()
  {
    Log.Info("Map started, join watch reset Map={Map} StuckJoins={StuckJoins} Pending={Pending}", Server.MapName, StuckJoins, Pending.Count);
    Pending.Clear();
    StuckJoins = 0;
    _mapStartedAt = DateTime.UtcNow;
    MapRefreshService.OnMapStart();
  }

  public static void Check(ExecutionMode mode = ExecutionMode.Clean)
  {
    MarkStuck(mode);

    var reason = AutoRestartRule.Reason(
      Enabled,
      Participants.Humans().Count,
      StuckJoins,
      Pending.Count,
      UptimeSeconds);

    if (reason != null)
      Restart(reason, mode);
  }

  public static string Restart(string reason, ExecutionMode mode = ExecutionMode.Clean)
  {
    var map = Server.MapName;

    if (Server.IsChangingLevel)
      return "A map change is already under way.";

    if (string.IsNullOrWhiteSpace(map))
    {
      Log.WithMode(mode).Warn("Restart skipped, map name unknown Reason={Reason}", reason);
      return "Map name unknown, not restarting.";
    }

    Log.WithMode(mode).Warn(
      "Reloading the map Reason={Reason} Map={Map} UptimeMinutes={UptimeMinutes} StuckJoins={StuckJoins} Pending={Pending}",
      reason, map, Math.Round(UptimeSeconds / 60), StuckJoins, Pending.Count);

    _lastReloadAt = DateTime.UtcNow;
    StuckJoins = 0;
    Server.ChangeLevel(map);
    return $"Reloading {map} ({reason}). Everyone connected reconnects by themselves.";
  }

  public static string SetEnabled(bool enabled, ExecutionMode mode = ExecutionMode.Clean)
  {
    Enabled = enabled;
    Log.WithMode(mode).Info("Auto restart set Enabled={Enabled}", enabled);
    BublockLog.Master.Info("Auto restart {State}", enabled ? "on" : "off");
    return $"Auto restart {(enabled ? "on" : "off")} until the next upload.";
  }

  public static IEnumerable<string> Describe()
  {
    var hours = UptimeSeconds / 3600;
    yield return $"Auto restart {(Enabled ? "on" : "off")}. Map {Server.MapName} up {hours:0.0} h (reload at {AutoRestartRule.MaxUptimeSeconds / 3600:0} h, or after a stuck join; only with nobody playing).";
    yield return $"Stuck joins since map start: {StuckJoins}. Joins in progress: {Pending.Count}.";
    yield return MapRefreshService.Describe();

    foreach (var (slot, join) in Pending.OrderBy(pair => pair.Key))
      yield return $"  slot {slot}: {join.Name} connecting for {(DateTime.UtcNow - join.Since).TotalSeconds:0} s{(join.Stuck ? " (stuck)" : "")}";
  }

  private static void MarkStuck(ExecutionMode mode)
  {
    foreach (var (slot, join) in Pending)
    {
      var seconds = (DateTime.UtcNow - join.Since).TotalSeconds;

      if (join.Stuck || !AutoRestartRule.IsStuck(seconds))
        continue;

      join.Stuck = true;
      StuckJoins++;
      Log.WithMode(mode).Warn(
        "Join stuck, client still connecting Name={Name} SteamId={SteamId} Slot={Slot} Seconds={Seconds} StuckJoins={StuckJoins}",
        join.Name, join.SteamId, slot, Math.Round(seconds), StuckJoins);
    }
  }
}
