namespace RiftRoulette.Lobby;

public static class PauseRule
{
  public const long UnpauseRetryMs = 2000;

  public const long TellCooldownMs = 5000;

  public static IReadOnlyList<string> Commands { get; } =
    ["pause", "setpause", "citadel_pause", "citadel_toggle_server_pause"];

  // citadel_pause_count and citadel_num_team_pauses_allowed are left alone: 0 there means unlimited.
  public static IReadOnlyList<(string Name, int Value)> ConVars(bool allowed) =>
  [
    ("citadel_allow_pausing", allowed ? 1 : 0),
    ("citadel_allow_pause_in_match", allowed ? 1 : 0),
    ("citadel_pause_allow_in_pregame", 0)
  ];

  public static bool IsPauseCommand(string? command) =>
    command != null && Commands.Contains(command.Trim(), StringComparer.OrdinalIgnoreCase);

  public static bool ShouldUnpause(bool paused, bool allowed, long nowMs, long? lastAttemptMs) =>
    paused && !allowed && (lastAttemptMs is not { } last || nowMs - last >= UnpauseRetryMs);

  public static bool ShouldTell(long nowMs, long? lastToldMs) =>
    lastToldMs is not { } last || nowMs - last >= TellCooldownMs;
}
