namespace RiftRoulette.Lobby;

public enum BanJoinDecision
{
  Statue,
  Refuse
}

public sealed record BanRecord(int Strikes, DateTime? LockedUntil)
{
  public static BanRecord None { get; } = new(0, null);

  public bool LockedForever => Strikes >= BanJoinRule.ForeverAfterStrikes;
}

public static class BanJoinRule
{
  public const int ForeverAfterStrikes = 3;

  public static readonly TimeSpan[] Lockouts = [TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(30)];

  public static BanJoinDecision Decide(BanRecord record, DateTime now) =>
    record.LockedForever || record.LockedUntil > now ? BanJoinDecision.Refuse : BanJoinDecision.Statue;

  public static BanRecord Strike(BanRecord record, DateTime now)
  {
    var strikes = record.Strikes + 1;

    return strikes >= ForeverAfterStrikes
      ? new BanRecord(strikes, null)
      : new BanRecord(strikes, now + Lockouts[strikes - 1]);
  }

  public static string Describe(BanRecord record, DateTime now) =>
    record.LockedForever ? $"Strikes={record.Strikes} locked until restart"
    : record.LockedUntil > now ? $"Strikes={record.Strikes} locked {Math.Ceiling((record.LockedUntil.Value - now).TotalMinutes)} min"
    : $"Strikes={record.Strikes}";
}
