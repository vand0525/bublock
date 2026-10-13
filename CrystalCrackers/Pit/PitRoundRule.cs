using System.Globalization;
using System.Numerics;

namespace CrystalCrackers.Pit;

public enum PitPhase
{
  Idle,
  Countdown,
  Live,
  BossDown
}

public enum PitEndReason
{
  WindowOver,
  TimeLimit,
  Admin
}

public enum RespawnAction
{
  None,
  SendUp,
  KeepIn
}

public static class PitRoundRule
{
  public const int AmberTeam = 2;
  public const int SapphireTeam = 3;

  public const int CountdownSeconds = 3;

  public const int DefaultWindowSeconds = 20;
  public const int MinWindowSeconds = 5;
  public const int MaxWindowSeconds = 120;

  public const int DefaultLimitSeconds = 300;
  public const int MinLimitSeconds = 30;
  public const int MaxLimitSeconds = 1800;

  // A reborn player farther than this (horizontal) from the pit center is put back at their start.
  public const float RebirthReturnDistance = 3000f;

  public static bool IsTeam(int team) => team is AmberTeam or SapphireTeam;

  public static int Other(int team) => team switch
  {
    AmberTeam => SapphireTeam,
    SapphireTeam => AmberTeam,
    _ => 0
  };

  public static string TeamName(int team) => team switch
  {
    AmberTeam => "Amber",
    SapphireTeam => "Sapphire",
    _ => "Nobody"
  };

  public static bool TryParseTeam(string text, out int team)
  {
    team = text.Trim().ToLowerInvariant() switch
    {
      "amber" => AmberTeam,
      "sapphire" => SapphireTeam,
      _ => 0
    };

    return team != 0;
  }

  // An admin's choice wins; otherwise the pit alternates, Amber first.
  public static int PitTeam(int chosen, int lastPitTeam) =>
    IsTeam(chosen) ? chosen
    : IsTeam(lastPitTeam) ? Other(lastPitTeam)
    : AmberTeam;

  // Earned souls and levels are blocked from the countdown until the round is over.
  public static bool BlocksSouls(PitPhase phase) => phase != PitPhase.Idle;

  public static bool IsFighting(PitPhase phase) => phase is PitPhase.Live or PitPhase.BossDown;

  // Only a finished crystal window has a winner: the crystal team, else the boss killer.
  public static int Winner(PitEndReason reason, int crystalTeam, int killerTeam) =>
    reason != PitEndReason.WindowOver ? 0
    : IsTeam(crystalTeam) ? crystalTeam
    : IsTeam(killerTeam) ? killerTeam
    : 0;

  // Dead fighters are out for the round, unless the crystal's rebirth brought them back.
  // A fighter's spawn without a death is a hero swap: the swap's own callback places them.
  public static RespawnAction OnRespawn(PitPhase phase, bool isFighter, bool died, bool reborn, bool roundsPlayed) =>
    phase switch
    {
      PitPhase.Countdown => RespawnAction.SendUp,
      PitPhase.Live or PitPhase.BossDown when !isFighter => RespawnAction.SendUp,
      PitPhase.Live or PitPhase.BossDown when !died => RespawnAction.None,
      PitPhase.Live or PitPhase.BossDown => reborn ? RespawnAction.KeepIn : RespawnAction.SendUp,
      _ => roundsPlayed ? RespawnAction.SendUp : RespawnAction.None
    };

  public static bool StartsInPit(int team, int pitTeam) => IsTeam(team) && team == pitTeam;

  public static string Score(IReadOnlyDictionary<int, int> wins) =>
    $"Sapphire {wins.GetValueOrDefault(SapphireTeam)} - Amber {wins.GetValueOrDefault(AmberTeam)}";

  public static string EndLine(PitEndReason reason) => reason switch
  {
    PitEndReason.WindowOver => "Nobody took the boss or the crystal",
    PitEndReason.TimeLimit => "Time is up, the boss survived",
    _ => "Round ended by an admin"
  };

  public static bool IsFarFromPit(Vector3 position) =>
    new Vector2(position.X, position.Y).Length() > RebirthReturnDistance;

  public static bool TryParseSeconds(string text, int min, int max, out int seconds) =>
    int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds)
      && seconds >= min
      && seconds <= max;
}
