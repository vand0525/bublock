using RiftRoulette.Lobby;

namespace Bublock.Tests.RiftRoulette;

public class BanJoinRuleTests
{
  private static readonly DateTime Now = new(2026, 9, 28, 2, 0, 0, DateTimeKind.Utc);

  [Fact]
  public void First_join_is_a_statue_visit()
  {
    Assert.Equal(BanJoinDecision.Statue, BanJoinRule.Decide(BanRecord.None, Now));
  }

  [Fact]
  public void Strikes_lock_out_for_10_then_30_minutes_then_until_restart()
  {
    var one = BanJoinRule.Strike(BanRecord.None, Now);
    Assert.Equal(1, one.Strikes);
    Assert.Equal(Now.AddMinutes(10), one.LockedUntil);

    var two = BanJoinRule.Strike(one, Now.AddMinutes(10));
    Assert.Equal(Now.AddMinutes(40), two.LockedUntil);

    var three = BanJoinRule.Strike(two, Now.AddMinutes(40));
    Assert.True(three.LockedForever);
    Assert.Null(three.LockedUntil);
  }

  [Fact]
  public void Joins_inside_a_lockout_are_refused_and_allowed_after_it()
  {
    var one = BanJoinRule.Strike(BanRecord.None, Now);

    Assert.Equal(BanJoinDecision.Refuse, BanJoinRule.Decide(one, Now.AddMinutes(9)));
    Assert.Equal(BanJoinDecision.Statue, BanJoinRule.Decide(one, Now.AddMinutes(10)));
  }

  [Fact]
  public void Third_strike_refuses_forever()
  {
    var record = BanRecord.None;

    for (var strike = 0; strike < BanJoinRule.ForeverAfterStrikes; strike++)
      record = BanJoinRule.Strike(record, Now);

    Assert.Equal(BanJoinDecision.Refuse, BanJoinRule.Decide(record, Now.AddDays(30)));
  }

  [Fact]
  public void Describe_shows_strikes_and_the_lockout()
  {
    var one = BanJoinRule.Strike(BanRecord.None, Now);

    Assert.Equal("Strikes=0", BanJoinRule.Describe(BanRecord.None, Now));
    Assert.Equal("Strikes=1 locked 10 min", BanJoinRule.Describe(one, Now));
    Assert.Equal("Strikes=1", BanJoinRule.Describe(one, Now.AddMinutes(11)));
    Assert.Equal("Strikes=3 locked until restart", BanJoinRule.Describe(new BanRecord(3, null), Now));
  }
}
