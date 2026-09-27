using RiftRoulette.Lobby;

namespace RiftRoulette.Balance;

public enum BalanceReason
{
  Stomp,
  Streak,
  Forced
}

public sealed record BalanceVerdict(int Team, BalanceReason Reason);

public sealed class BalanceTracker
{
  public const int LeadRounds = 2;
  public const int StompKillDiff = 8;
  public const double StompKillRatio = 1.5;
  public const int StreakRounds = 5;

  public int SapphireRounds { get; private set; }
  public int AmberRounds { get; private set; }
  public int SapphireKills { get; private set; }
  public int AmberKills { get; private set; }
  public int StreakTeam { get; private set; }
  public int Streak { get; private set; }

  public void RecordRound(int? pointTo)
  {
    if (pointTo is not { } team || !RiftRouletteTeams.IsPlayable(team))
      return;

    if (team == RiftRouletteTeams.Sapphire)
      SapphireRounds++;
    else
      AmberRounds++;

    if (team == StreakTeam)
    {
      Streak++;
      return;
    }

    StreakTeam = team;
    Streak = 1;
  }

  public void RecordKill(int team)
  {
    if (team == RiftRouletteTeams.Sapphire)
      SapphireKills++;
    else if (team == RiftRouletteTeams.Amber)
      AmberKills++;
  }

  public BalanceVerdict? Check()
  {
    var lead = SapphireRounds - AmberRounds;

    if (Math.Abs(lead) >= LeadRounds)
    {
      var leader = lead > 0 ? RiftRouletteTeams.Sapphire : RiftRouletteTeams.Amber;
      var leaderKills = KillsOf(leader);
      var otherKills = KillsOf(RiftRouletteTeams.Other(leader));

      if (leaderKills - otherKills >= StompKillDiff && leaderKills >= otherKills * StompKillRatio)
        return new BalanceVerdict(leader, BalanceReason.Stomp);
    }

    if (Streak >= StreakRounds)
      return new BalanceVerdict(StreakTeam, BalanceReason.Streak);

    return null;
  }

  public int Leader()
  {
    if (SapphireRounds != AmberRounds)
      return SapphireRounds > AmberRounds ? RiftRouletteTeams.Sapphire : RiftRouletteTeams.Amber;

    return AmberKills > SapphireKills ? RiftRouletteTeams.Amber : RiftRouletteTeams.Sapphire;
  }

  public void Reset()
  {
    SapphireRounds = 0;
    AmberRounds = 0;
    SapphireKills = 0;
    AmberKills = 0;
    StreakTeam = 0;
    Streak = 0;
  }

  public string Describe() =>
    $"Since last swap: Rounds Sapphire {SapphireRounds} - {AmberRounds} Amber | " +
    $"Kills Sapphire {SapphireKills} - {AmberKills} Amber | " +
    $"Streak={(Streak == 0 ? "-" : $"{RiftRouletteTeams.Name(StreakTeam)} x{Streak}")}";

  private int KillsOf(int team) => team == RiftRouletteTeams.Sapphire ? SapphireKills : AmberKills;
}
