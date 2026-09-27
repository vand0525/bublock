using RiftRoulette.Lobby;
using RiftRoulette.Rift;

namespace RiftRoulette.GameLoop;

public enum MatchPhase
{
  Idle,
  Intermission,
  InRound
}

public sealed class MatchState
{
  public MatchPhase Phase { get; private set; } = MatchPhase.Idle;
  public int Round { get; private set; }
  public int Sapphire { get; private set; }
  public int Amber { get; private set; }
  public int Ties { get; private set; }

  public bool IsRunning => Phase != MatchPhase.Idle;

  public void Start()
  {
    Reset();
    Phase = MatchPhase.Intermission;
  }

  public void BeginRound()
  {
    Round++;
    Phase = MatchPhase.InRound;
  }

  public void EnterIntermission()
  {
    Phase = MatchPhase.Intermission;
  }

  public int? Apply(RiftRoundResult result)
  {
    if (result.Outcome == RiftOutcome.Tied)
      Ties++;

    if (result.Outcome != RiftOutcome.Finished)
      return null;

    switch (result.WinnerTeam)
    {
      case RiftRouletteTeams.Sapphire:
        Sapphire++;
        return RiftRouletteTeams.Sapphire;

      case RiftRouletteTeams.Amber:
        Amber++;
        return RiftRouletteTeams.Amber;

      default:
        return null;
    }
  }

  public string FormatScore() => $"Sapphire {Sapphire} - {Amber} Amber";

  public static string DescribeResult(RiftRoundResult result, int? pointTo) => result.Outcome switch
  {
    RiftOutcome.Finished when pointTo is { } team => $"{RiftRouletteTeams.Name(team)} took the rift",
    RiftOutcome.Finished => "Rift taken, winner unknown - no point",
    RiftOutcome.Tied => "Tied - no point",
    RiftOutcome.Cancelled => "Round cancelled - no point",
    RiftOutcome.SpawnTimedOut => "Rift did not spawn - no point",
    _ => $"{result.Outcome} - no point"
  };

  public void Reset()
  {
    Phase = MatchPhase.Idle;
    Round = 0;
    Sapphire = 0;
    Amber = 0;
    Ties = 0;
  }
}
