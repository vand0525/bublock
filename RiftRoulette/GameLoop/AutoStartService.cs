using Bublock.Modules.Hud;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Duel;
using RiftRoulette.Lobby;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.GameLoop;

public static class AutoStartService
{
  public const int MinPlayers = AutoStartRule.DefaultMinPlayers;

  private const int WaitingBannerDelaySeconds = 3;

  public const int JoinCheckDelaySeconds = 2;

  private static readonly Logger Log = BublockLog.For("Match");

  public static bool Enabled { get; private set; } = true;

  public static void SetEnabled(bool enabled, ExecutionMode mode = ExecutionMode.Clean)
  {
    Enabled = enabled;
    Log.WithMode(mode).Info("Auto-start set Enabled={Enabled}", enabled);
  }

  public static AutoStartAction Check(ITimer timer, ExecutionMode mode = ExecutionMode.Clean, ulong? leavingSteamId = null)
  {
    var log = Log.WithMode(mode);
    var humans = MatchConfig.IsDuel ? DuelService.QueuedCount(leavingSteamId) : Humans(leavingSteamId).Count;
    var leaving = leavingSteamId != null;
    var action = AutoStartRule.Decide(Enabled, MatchService.State.IsRunning, humans, MinPlayers, leaving);

    log.Debug(
      "Auto-start check Enabled={Enabled} Running={Running} Humans={Humans} Leaving={Leaving} Action={Action}",
      Enabled,
      MatchService.State.IsRunning,
      humans,
      leaving,
      action);

    switch (action)
    {
      case AutoStartAction.Start when MatchConfig.IsDuel && !DuelService.HasSnapshot:
        log.Debug("Auto-start waiting for a 1v1 build Humans={Humans}", humans);
        return AutoStartAction.None;

      case AutoStartAction.Start:
        var started = MatchService.Start(timer, mode);

        if (!MatchService.State.IsRunning)
        {
          log.Info("Auto-start refused Humans={Humans} Reply={Reply}", humans, started);
          return AutoStartAction.None;
        }

        log.Info("Match auto-started Humans={Humans}", humans);
        BublockLog.Master.Info("Match auto-started Humans={Humans}", humans);
        return action;

      case AutoStartAction.End:
        var ended = MatchService.End(timer, mode);

        log.Info("Match auto-ended Humans={Humans} Reply={Reply}", humans, ended);
        BublockLog.Master.Info("Match auto-ended Humans={Humans}", humans);

        timer.Once(WaitingBannerDelaySeconds.Seconds(), () => AnnounceWaiting(mode));
        return action;

      default:
        return action;
    }
  }

  public static void CheckSoon(ITimer timer, ExecutionMode mode = ExecutionMode.Clean) =>
    timer.Once(JoinCheckDelaySeconds.Seconds(), () => Check(timer, mode));

  public static string Describe() =>
    $"Auto-start={(Enabled ? "on" : "off")} | MinPlayers={MinPlayers} | Humans={Humans(null).Count}" +
    (MatchConfig.IsDuel ? $" | Queued={DuelService.QueuedCount()} (1v1 counts the queue)" : "");

  private static void AnnounceWaiting(ExecutionMode mode)
  {
    if (MatchService.State.IsRunning)
      return;

    foreach (var player in Humans(null))
      HudService.Announce(
        player,
        "Waiting for players",
        MatchConfig.IsDuel
          ? $"1v1 starts when {MinPlayers} are queued - /queue"
          : $"Match starts at {MinPlayers} players",
        mode);
  }

  private static List<CCitadelPlayerController> Humans(ulong? leavingSteamId) =>
    Participants.Humans()
      .Where(player => player.PlayerSteamId != leavingSteamId)
      .ToList();
}
