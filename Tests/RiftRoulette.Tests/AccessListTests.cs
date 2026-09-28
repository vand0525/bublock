using System.Text.Json;
using RiftRoulette.Lobby;

namespace Bublock.Tests.RiftRoulette;

public class AccessListTests
{
  [Fact]
  public void Json_round_trips_mode_and_sorted_lists()
  {
    var list = new AccessList { Private = true };
    list.Banned.Add(76561198000000002);
    list.Banned.Add(76561198000000001);
    list.Allowed.Add(76561198192980843);

    var json = list.ToJson();
    var parsed = AccessList.Parse(json);

    Assert.Contains("\"private\": true", json);
    Assert.True(parsed.Private);
    Assert.Equal([76561198000000001UL, 76561198000000002UL], parsed.Banned);
    Assert.Equal([76561198192980843UL], parsed.Allowed);
  }

  [Fact]
  public void Parse_accepts_hand_edits()
  {
    const string json = """
      {
        // edited by hand
        "Private": false,
        "banned": ["76561198000000001", 76561198000000002,],
      }
      """;

    var parsed = AccessList.Parse(json);

    Assert.False(parsed.Private);
    Assert.Equal(2, parsed.Banned.Count);
    Assert.Empty(parsed.Allowed);
  }

  [Fact]
  public void Statue_modifier_round_trips_and_is_left_out_when_unset()
  {
    var unset = new AccessList().ToJson();
    var list = new AccessList { StatueModifier = "modifier_example" };
    var parsed = AccessList.Parse(list.ToJson());

    Assert.DoesNotContain("statueModifier", unset);
    Assert.Null(AccessList.Parse(unset).StatueModifier);
    Assert.Equal("modifier_example", parsed.StatueModifier);
    Assert.Null(AccessList.Parse("""{ "statueModifier": "  " }""").StatueModifier);
  }

  [Fact]
  public void Parse_rejects_bad_json()
  {
    Assert.ThrowsAny<JsonException>(() => AccessList.Parse("{ not json"));
    Assert.ThrowsAny<JsonException>(() => AccessList.Parse("null"));
  }

  [Fact]
  public void Check_uses_lists_and_mode()
  {
    var list = new AccessList();
    list.Banned.Add(1);
    list.Allowed.Add(2);

    Assert.Equal(AccessVerdict.Banned, list.Check(1, isAdmin: true));
    Assert.Equal(AccessVerdict.Allowed, list.Check(3, isAdmin: false));

    list.Private = true;

    Assert.Equal(AccessVerdict.Allowed, list.Check(2, isAdmin: false));
    Assert.Equal(AccessVerdict.Private, list.Check(3, isAdmin: false));
    Assert.Equal(AccessVerdict.Allowed, list.Check(3, isAdmin: true));
  }
}
