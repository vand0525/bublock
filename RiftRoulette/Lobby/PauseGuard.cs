using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.SelfTest;

namespace RiftRoulette.Lobby;

public static class PauseGuard
{
  public const string OffMessage = "Pausing is off on this server.";

  private static readonly Logger Log = BublockLog.For("Pause");

  private static readonly Dictionary<ulong, long> LastTold = [];

  private static long? _lastUnpauseAttempt;

  public static bool Allowed { get; private set; }

  public static int Blocked { get; private set; }

  public static int Unpauses { get; private set; }

  public static void Apply(ExecutionMode mode = ExecutionMode.Clean)
  {
    foreach (var (name, value) in PauseRule.ConVars(Allowed))
      ServerConVars.TrySet(name, value, Log);

    Log.WithMode(mode).Info("Pause convars applied Allowed={Allowed}", Allowed);
  }

  // A private server (an organised event) may pause; an open one may not.
  public static void FollowAccess(ExecutionMode mode = ExecutionMode.Clean)
  {
    Allowed = AccessService.Load().Private;
    Apply(mode);
  }

  public static string SetAllowed(bool allowed, ExecutionMode mode = ExecutionMode.Clean)
  {
    Allowed = allowed;
    Apply(mode);
    BublockLog.Master.Info("Pausing turned {State}", allowed ? "on" : "off");
    return $"Pausing is {(allowed ? "on" : "off")}.";
  }

  public static bool Block(int slot, string source, string detail)
  {
    if (Allowed)
      return false;

    var player = Players.GetAll().FirstOrDefault(candidate => candidate.Slot == slot);
    return Block(player, source, detail, slot);
  }

  public static bool Block(CCitadelPlayerController? player, string source, string detail) =>
    !Allowed && Block(player, source, detail, player?.Slot ?? -1);

  public static void Tick()
  {
    var now = Environment.TickCount64;

    if (!PauseRule.ShouldUnpause(GameRules.GamePaused, Allowed, now, _lastUnpauseAttempt))
      return;

    _lastUnpauseAttempt = now;
    Unpauses++;
    EventCounters.Hit("pause_auto_unpause");
    Server.ExecuteCommand("pause");
    Log.Warn("Game paused while pausing is off, sent pause toggle Attempt={Attempt} ServerPaused={ServerPaused}", Unpauses, GameRules.ServerPaused);
  }

  public static IReadOnlyList<string> Describe() =>
  [
    $"Pausing {(Allowed ? "on" : "off")} | GamePaused={GameRules.GamePaused} | ServerPaused={GameRules.ServerPaused} | " +
    $"Blocked={Blocked} | AutoUnpauses={Unpauses}"
  ];

  private static bool Block(CCitadelPlayerController? player, string source, string detail, int slot)
  {
    Blocked++;
    EventCounters.Hit($"pause_blocked_{source}");

    if (player == null)
    {
      Log.Info("Pause blocked Source={Source} Detail={Detail} Slot={Slot}", source, detail, slot);
      return true;
    }

    Log.Info(player.ToPlayerRef(), "Pause blocked Source={Source} Detail={Detail}", source, detail);

    var now = Environment.TickCount64;
    var steamId = player.PlayerSteamId;

    if (PauseRule.ShouldTell(now, LastTold.TryGetValue(steamId, out var last) ? last : null))
    {
      LastTold[steamId] = now;
      PlayerChat.Send(player, OffMessage);
    }

    return true;
  }
}
