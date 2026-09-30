using System.Numerics;
using Bublock.Modules.Movement;
using RiftRoulette.Lobby;
using RiftRoulette.Rift;

namespace RiftRoulette.Tests;

public class StreamFramingTests
{
  private static readonly MovementLocation Green = new("green", new Vector3(5000f, 0f, 1536f), new Vector3(0f, 180f, 0f));
  private static readonly MovementLocation Yellow = new("yellow", new Vector3(-5000f, 0f, 1536f), new Vector3(0f, 0f, 0f));

  private static void Near(Vector3 expected, Vector3 actual) =>
    Assert.True(Vector3.Distance(expected, actual) < 0.01f, $"Expected {expected}, got {actual}");

  [Fact]
  public void Default_is_straight_down_264_above_the_anchor_facing_its_yaw()
  {
    var (position, angle) = StreamFraming.ToWorld(Green, StreamFraming.Default);

    Near(new Vector3(5000f, 0f, 1800f), position);
    Assert.Equal(89f, angle.X);
    Assert.Equal(-180f, angle.Y, 3);
  }

  [Fact]
  public void FromWorld_then_ToWorld_gives_back_the_same_view()
  {
    var position = new Vector3(4700f, 250f, 1900f);
    var angle = new Vector3(40f, 150f, 0f);

    var (back, backAngle) = StreamFraming.ToWorld(Green, StreamFraming.FromWorld(Green, position, angle));

    Near(position, back);
    Assert.Equal(40f, backAngle.X, 3);
    Assert.Equal(150f, backAngle.Y, 3);
  }

  [Fact]
  public void A_framing_saved_on_one_side_is_mirrored_on_the_other()
  {
    var pose = StreamFraming.FromWorld(Green, new Vector3(4700f, 250f, 1900f), new Vector3(40f, 150f, 0f));

    var (position, angle) = StreamFraming.ToWorld(Yellow, pose);

    Near(new Vector3(-4700f, -250f, 1900f), position);
    Assert.Equal(-30f, angle.Y, 3);
  }

  [Fact]
  public void Pick_prefers_the_side_then_the_other_side_then_the_default()
  {
    var green = new CameraPose(new Vector3(1f, 2f, 3f), 30f, 10f);
    var yellow = new CameraPose(new Vector3(4f, 5f, 6f), 40f, 20f);

    Assert.Equal(yellow, StreamFraming.Pick(new Dictionary<RiftSide, CameraPose> { [RiftSide.Green] = green, [RiftSide.Yellow] = yellow }, RiftSide.Yellow));
    Assert.Equal(green, StreamFraming.Pick(new Dictionary<RiftSide, CameraPose> { [RiftSide.Green] = green }, RiftSide.Yellow));
    Assert.Equal(StreamFraming.Default, StreamFraming.Pick(new Dictionary<RiftSide, CameraPose>(), RiftSide.Green));
  }

  [Fact]
  public void Pick_center_does_not_mirror_green()
  {
    var green = new CameraPose(new Vector3(1f, 2f, 3f), 30f, 10f);

    Assert.Equal(StreamFraming.Default, StreamFraming.Pick(new Dictionary<RiftSide, CameraPose> { [RiftSide.Green] = green }, RiftSide.Center));
    Assert.Equal(green, StreamFraming.Pick(new Dictionary<RiftSide, CameraPose> { [RiftSide.Center] = green }, RiftSide.Center));
  }

  [Fact]
  public void Serialize_and_Parse_round_trip()
  {
    var poses = new Dictionary<RiftSide, CameraPose>
    {
      [RiftSide.Green] = new(new Vector3(-300f, 250f, 364f), 40f, -30f),
      [RiftSide.Yellow] = StreamFraming.Default
    };

    var back = StreamFraming.Parse(StreamFraming.Serialize(poses));

    Assert.Equal(poses[RiftSide.Green], back[RiftSide.Green]);
    Assert.Equal(poses[RiftSide.Yellow], back[RiftSide.Yellow]);
  }

  [Fact]
  public void Parse_skips_unknown_sides_and_bad_offsets()
  {
    var back = StreamFraming.Parse("""{"green":{"offset":[1,2],"pitch":1,"yaw":2},"blue":{"offset":[1,2,3],"pitch":1,"yaw":2},"yellow":{"offset":[1,2,3],"pitch":45,"yaw":5}}""");

    Assert.Single(back);
    Assert.Equal(new CameraPose(new Vector3(1f, 2f, 3f), 45f, 5f), back[RiftSide.Yellow]);
  }
}
