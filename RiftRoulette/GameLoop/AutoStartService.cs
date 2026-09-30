using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Duel;
using RiftRoulette.Lobby;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.GameLoop;

public static class AutoStartService
{
  public const int MinPlayers = AutoStartRule.DefaultMinPlayers;

  private const int WaitingMessageDelaySeconds = 3;

  public const int JoinCheckDelaySeconds = 2;

  public const int WaitingReminderSeconds = 30;

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
      case AutoStartAction.Start when MapRefreshService.Pending:
        log.Debug("Auto-start waiting for the refresh reload Humans={Humans}", humans);
        return AutoStartAction.None;

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

        timer.Once(WaitingMessageDelaySeconds.Seconds(), () => AnnounceWaiting(mode));
        return action;

      default:
        if (!leaving && IsWaiting(humans))
          AnnounceWaiting(mode);

        return action;
    }
  }

  public static void RemindWaiting(ExecutionMode mode = ExecutionMode.Clean)
  {
    if (IsWaiting(MatchConfig.IsDuel ? DuelService.QueuedCount() : Humans(null).Count))
      AnnounceWaiting(mode);
  }

  private static bool IsWaiting(int humans) =>
    Enabled && !MatchService.State.IsRunning && !MapRefreshService.Pending && humans > 0 && humans < MinPlayers;

  public static void CheckSoon(ITimer timer, ExecutionMode mode = ExecutionMode.Clean) =>
    timer.Once(JoinCheckDelaySeconds.Seconds(), () => Check(timer, mode));

  public static string Describe() =>
    $"Auto-start={(Enabled ? "on" : "off")} | MinPlayers={MinPlayers} | Humans={Humans(null).Count}" +
    (MatchConfig.IsDuel ? $" | Queued={DuelService.QueuedCount()} (1v1 counts the queue)" : "");

  // Chat, not a banner: banners go by too fast and long lines don't fit.
  private static void AnnounceWaiting(ExecutionMode mode)
  {
    if (MatchService.State.IsRunning)
      return;

    var line = $"Waiting for players: {WaitingDescription()}. You wait up top until then.";

    foreach (var player in Humans(null))
      PlayerChat.Send(player, line);

    Log.WithMode(mode).Debug("Waiting announced Line={Line}", line);
  }

  private static string WaitingDescription()
  {
    if (MatchConfig.IsDuel)
      return $"1v1 starts when {MinPlayers} are queued - /queue";

    var missing = Math.Max(1, MinPlayers - Humans(null).Count);
    return missing == 1 ? "Match starts when 1 more player joins" : $"Match starts when {missing} more players join";
  }

  private static List<CCitadelPlayerController> Humans(ulong? leavingSteamId) =>
    Participants.Humans()
      .Where(player => player.PlayerSteamId != leavingSteamId)
      .ToList();
}
