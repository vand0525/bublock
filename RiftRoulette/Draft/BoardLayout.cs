using Bublock.Modules.WorldText;
using RiftRoulette.Lobby;
using RiftRoulette.Round;
using System.Numerics;

namespace RiftRoulette.Draft;

public static class BoardLayout
{
  public const float SideFontScale = 0.8f;
  public const float NoteFontScale = 1.2f;
  public const float NoteDrop = 120f;

  private static readonly Vector3 SapphireOffset = new(-90f, 500f, 0f);
  private static readonly Vector3 AmberOffset = new(90f, -500f, 0f);
  private static readonly Vector3 BackOffset = new(-500f, 0f, 0f);

  private static readonly Vector3 WelcomeAngle = new(0f, -90f, 90f);
  private static readonly Vector3 SapphireAngle = new(0f, 360f, 90f);
  private static readonly Vector3 AmberAngle = new(0f, 180f, 90f);
  private static readonly Vector3 BackAngle = new(0f, 90f, 90f);

  private static readonly WorldTextColor WelcomeColor = new(255, 255, 255, 255);
  private static readonly WorldTextColor SapphireColor = new(0, 150, 255, 255);
  private static readonly WorldTextColor AmberColor = new(255, 70, 0, 255);

  public static Vector3 Origin => WatchSpot.Location(WatchSpot.BoardSide).Position;

  public static WorldTextSpec Welcome(string text) =>
    Place(text, WatchLayout.WelcomeOffset, WelcomeAngle, WelcomeColor, 3f);

  public static WorldTextSpec Hint(string text) =>
    Place(text, WatchLayout.WelcomeOffset - new Vector3(0f, 0f, NoteDrop), WelcomeAngle, WelcomeColor, NoteFontScale);

  public static WorldTextSpec Note(string text) =>
    Place(text, WatchLayout.WelcomeOffset - new Vector3(0f, 0f, 2f * NoteDrop), WelcomeAngle, WelcomeColor, NoteFontScale);

  public static WorldTextSpec Side(int team, string text) =>
    team == RiftRouletteTeams.Sapphire
      ? Place(text, SapphireOffset, SapphireAngle, SapphireColor, SideFontScale)
      : Place(text, AmberOffset, AmberAngle, AmberColor, SideFontScale);

  // The empty side behind the watch view, between the side boards; board yaw = wall direction - 90, like the side boards.
  public static WorldTextSpec Back(string text) =>
    Place(text, BackOffset, BackAngle, WelcomeColor, SideFontScale);

  private static WorldTextSpec Place(string text, Vector3 greenOffset, Vector3 greenAngle, WorldTextColor color, float scale)
  {
    var side = WatchSpot.BoardSide;
    var origin = WatchSpot.Location(side).Position;

    return new(text, origin + WatchLayout.Offset(side, greenOffset), WatchLayout.Angle(side, greenAngle), color, scale);
  }
}
