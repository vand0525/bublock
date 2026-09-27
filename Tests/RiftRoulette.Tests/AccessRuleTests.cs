using RiftRoulette.Lobby;

namespace Bublock.Tests.RiftRoulette;

public class AccessRuleTests
{
  [Theory]
  [InlineData(true, false, false, false, AccessVerdict.Banned)]
  [InlineData(true, true, true, true, AccessVerdict.Banned)]
  [InlineData(false, false, false, false, AccessVerdict.Allowed)]
  [InlineData(false, true, false, false, AccessVerdict.Private)]
  [InlineData(false, true, true, false, AccessVerdict.Allowed)]
  [InlineData(false, true, false, true, AccessVerdict.Allowed)]
  public void Check_bans_first_then_private_mode(bool banned, bool privateMode, bool allowed, bool isAdmin, AccessVerdict expected)
  {
    Assert.Equal(expected, AccessRule.Check(banned, privateMode, allowed, isAdmin));
  }

  [Theory]
  [InlineData("76561198192980843", true)]
  [InlineData(" 76561198192980843 ", true)]
  [InlineData("3", false)]
  [InlineData("76561197960265728", false)]
  [InlineData("not-an-id", false)]
  [InlineData("", false)]
  public void TryParseSteamId_accepts_only_individual_steam64_ids(string text, bool expected)
  {
    Assert.Equal(expected, AccessRule.TryParseSteamId(text, out _));
  }
}
