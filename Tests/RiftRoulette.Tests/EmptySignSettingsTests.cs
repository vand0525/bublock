using System.Text.Json;
using RiftRoulette.Boards;

namespace RiftRoulette.Tests;

public class EmptySignSettingsTests
{
  [Fact]
  public void Defaults_are_the_tuned_sign()
  {
    var settings = EmptySignSettings.Default;

    Assert.Equal(300f, settings.Distance);
    Assert.Equal(100f, settings.Shift);
    Assert.Equal(0f, settings.Height);
    Assert.Equal(1.2f, settings.TitleScale);
    Assert.Equal(0.45f, settings.JoinScale);
    Assert.Equal(8f, settings.Gap);
    Assert.Equal(new SignColor(0, 190, 0), settings.TitleColor);
    Assert.Equal(new SignColor(0, 190, 0), settings.JoinColor);
    Assert.Equal("RIFT ROULETTE", settings.TitleText);
    Assert.Equal("Join with connect 66.94.102.245:27015", settings.JoinText);
    Assert.Null(settings.Problem());
  }

  [Theory]
  [InlineData(10f)]
  [InlineData(5000f)]
  public void Distance_out_of_range_is_a_problem(float distance) =>
    Assert.NotNull((EmptySignSettings.Default with { Distance = distance }).Problem());

  [Fact]
  public void Offsets_scales_gap_colors_and_text_are_range_checked()
  {
    var settings = EmptySignSettings.Default;

    Assert.NotNull((settings with { Shift = -4000f }).Problem());
    Assert.NotNull((settings with { Height = 4000f }).Problem());
    Assert.NotNull((settings with { TitleScale = 0f }).Problem());
    Assert.NotNull((settings with { JoinScale = 11f }).Problem());
    Assert.NotNull((settings with { Gap = -1f }).Problem());
    Assert.NotNull((settings with { TitleColor = new SignColor(256, 0, 0) }).Problem());
    Assert.NotNull((settings with { JoinColor = new SignColor(0, -1, 0) }).Problem());
    Assert.NotNull((settings with { JoinText = new string('x', 121) }).Problem());
    Assert.Null((settings with { Shift = -3000f, Height = 3000f, Gap = 0f }).Problem());
  }

  [Fact]
  public void Json_round_trips()
  {
    var settings = EmptySignSettings.Default with
    {
      Distance = 450f,
      Shift = -50f,
      Height = 12.5f,
      TitleColor = new SignColor(10, 20, 30),
      JoinText = "Join us"
    };

    Assert.Equal(settings, EmptySignSettings.FromJson(settings.ToJson()));
  }

  [Fact]
  public void Missing_json_fields_keep_their_defaults()
  {
    var settings = EmptySignSettings.FromJson("{\"distance\": 250}");

    Assert.Equal(250f, settings.Distance);
    Assert.Equal(EmptySignSettings.Default with { Distance = 250f }, settings);
  }

  [Fact]
  public void Null_or_broken_json_throws()
  {
    Assert.Throws<JsonException>(() => EmptySignSettings.FromJson("null"));
    Assert.ThrowsAny<JsonException>(() => EmptySignSettings.FromJson("{ not json"));
  }

  [Fact]
  public void Color_converts_to_opaque_world_text_color()
  {
    var color = new SignColor(0, 190, 0).ToWorldText();

    Assert.Equal((byte)0, color.R);
    Assert.Equal((byte)190, color.G);
    Assert.Equal((byte)0, color.B);
    Assert.Equal((byte)255, color.A);
  }
}
