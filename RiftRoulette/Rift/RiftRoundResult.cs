namespace RiftRoulette.Rift;

public static class RiftOutcome
{
  public const string Finished = "finished";
  public const string Tied = "tied";
  public const string Cancelled = "cancelled";
  public const string SpawnTimedOut = "spawn timed out";
}

public sealed record RiftRoundResult(string Outcome, RiftSide? Side, int? WinnerTeam);
