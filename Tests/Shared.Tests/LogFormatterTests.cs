using Bublock.Shared;

namespace Shared.Tests;

public class LogFormatterTests
{
  private static readonly DateTime Noon = new(2026, 9, 26, 12, 0, 0, 123, DateTimeKind.Utc);

  [Fact]
  public void Format_WritesUtcTimestampLevelPrefixAndCorrelation()
  {
    var line = LogFormatter.Format(Noon, LogLevel.Information, "RiftRoulette.Draft", "a1b2c3d4", null, null, "hello");

    Assert.Equal("2026-09-26T12:00:00.123Z [Information] [RiftRoulette.Draft] session=a1b2c3d4 round=- | hello", line);
  }

  [Fact]
  public void Format_ConvertsLocalTimeToUtc()
  {
    var local = Noon.ToLocalTime();

    var line = LogFormatter.Format(local, LogLevel.Debug, "X", "s", null, null, "m");

    Assert.StartsWith("2026-09-26T12:00:00.123Z", line);
  }

  [Fact]
  public void Format_IncludesPlayerAndRound()
  {
    var player = new PlayerRef(3, 76561198192980843, "Theo \"T\"");

    var line = LogFormatter.Format(Noon, LogLevel.Warning, "RiftRoulette.Draft", "s", "r7", player, "m");

    Assert.Contains("round=r7 player=\"Theo 'T'\" steam=76561198192980843 slot=3 | m", line);
  }

  [Fact]
  public void Format_AppendsExceptionStack()
  {
    var line = LogFormatter.Format(Noon, LogLevel.Error, "X", "s", null, null, "boom", new InvalidOperationException("bad"));

    Assert.Contains("| boom" + Environment.NewLine + "System.InvalidOperationException: bad", line);
  }

  [Fact]
  public void RenderTemplate_ReplacesPlaceholdersAsNameValue()
  {
    var text = LogFormatter.RenderTemplate("Selected hero {Hero} at {Slot}", ["Shiv", 3]);

    Assert.Equal("Selected hero Hero=Shiv at Slot=3", text);
  }

  [Fact]
  public void RenderTemplate_DoesNotRepeatNameWrittenInTemplate()
  {
    var text = LogFormatter.RenderTemplate("Selected hero Hero={Hero} XSlot={Slot}", ["Shiv", 3]);

    Assert.Equal("Selected hero Hero=Shiv XSlot=Slot=3", text);
  }

  [Fact]
  public void RenderTemplate_QuotesStringsWithSpacesAndHandlesNull()
  {
    var text = LogFormatter.RenderTemplate("{Name} {Other} {Empty}", ["Big Theo", null, ""]);

    Assert.Equal("Name=\"Big Theo\" Other=null Empty=\"\"", text);
  }

  [Fact]
  public void RenderTemplate_UsesInvariantCulture()
  {
    var text = LogFormatter.RenderTemplate("{Value}", [1234.5]);

    Assert.Equal("Value=1234.5", text);
  }

  [Fact]
  public void RenderTemplate_KeepsMissingPlaceholdersAndEscapes()
  {
    var text = LogFormatter.RenderTemplate("{{literal}} {A} {B}", [1]);

    Assert.Equal("{literal} A=1 {B}", text);
  }
}
