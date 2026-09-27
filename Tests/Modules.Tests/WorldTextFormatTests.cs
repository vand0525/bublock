using Bublock.Modules.WorldText;

namespace Bublock.Tests.Modules;

public class WorldTextFormatTests
{
  [Fact]
  public void FromArgs_joins_with_single_spaces()
  {
    Assert.Equal("Welcome to Rift Roulette", WorldTextFormat.FromArgs(["Welcome", "to", "Rift", "Roulette"]));
  }

  [Fact]
  public void FromArgs_turns_backslash_n_into_line_break()
  {
    Assert.Equal("Line one\nLine two", WorldTextFormat.FromArgs(["Line one\\nLine two"]));
  }

  [Fact]
  public void FromArgs_of_nothing_is_empty()
  {
    Assert.Equal("", WorldTextFormat.FromArgs([]));
  }

  [Fact]
  public void Preview_keeps_short_text()
  {
    Assert.Equal("RIFT ROULETTE", WorldTextFormat.Preview("RIFT ROULETTE"));
  }

  [Fact]
  public void Preview_flattens_line_breaks()
  {
    Assert.Equal("SAPPHIRE /  / Shiv", WorldTextFormat.Preview("SAPPHIRE\n\nShiv"));
  }

  [Fact]
  public void Preview_truncates_long_text()
  {
    var preview = WorldTextFormat.Preview(new string('a', 50), 10);

    Assert.Equal("aaaaaaaaaa...", preview);
  }
}
