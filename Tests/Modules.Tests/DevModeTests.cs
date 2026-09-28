using Bublock.Modules.DevMode;

namespace Bublock.Tests.Modules;

public class DevModeTests
{
  [Fact]
  public void Dev_only_commands_are_allowed_in_dev_and_refused_in_prod()
  {
    Assert.Null(DevRules.Refusal(RunMode.Dev, "gg_bots"));
    Assert.Equal("gg_bots is dev-only; a live session is running (/stop first).", DevRules.Refusal(RunMode.Prod, "gg_bots"));
  }

  [Theory]
  [InlineData("dev", RunMode.Dev, true)]
  [InlineData(" PROD ", RunMode.Prod, true)]
  [InlineData("staging", RunMode.Dev, false)]
  public void Mode_names_parse(string text, RunMode expected, bool ok)
  {
    Assert.Equal(ok, DevRules.TryParse(text, out var mode));
    Assert.Equal(expected, mode);
    Assert.Equal(expected == RunMode.Dev ? "dev" : "prod", DevRules.Name(expected));
  }
}
