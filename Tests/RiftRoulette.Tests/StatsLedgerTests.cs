using RiftRoulette.Lobby;
using RiftRoulette.Stats;

namespace Bublock.Tests.RiftRoulette;

public class StatsLedgerTests
{
  private static readonly Participant Victim = new(1, RiftRouletteTeams.Amber);
  private static readonly Participant Killer = new(2, RiftRouletteTeams.Sapphire);
  private static readonly Participant Helper = new(3, RiftRouletteTeams.Sapphire);
  private static readonly Participant Enemy = new(4, RiftRouletteTeams.Amber);

  [Fact]
  public void Enemy_kill_credits_killer_victim_and_assisters()
  {
    var ledger = new StatsLedger();

    var team = ledger.RecordDeath(Victim, Killer, [Helper]);

    Assert.Equal(RiftRouletteTeams.Sapphire, team);
    Assert.Equal(new PlayerStats(0, 1, 0), ledger.Get(Victim.SteamId));
    Assert.Equal(new PlayerStats(1, 0, 0), ledger.Get(Killer.SteamId));
    Assert.Equal(new PlayerStats(0, 0, 1), ledger.Get(Helper.SteamId));
  }

  [Fact]
  public void Death_without_attacker_counts_only_the_death()
  {
    var ledger = new StatsLedger();

    Assert.Null(ledger.RecordDeath(Victim, null, [Helper]));
    Assert.Equal(new PlayerStats(0, 1, 0), ledger.Get(Victim.SteamId));
    Assert.Equal(PlayerStats.Empty, ledger.Get(Helper.SteamId));
  }

  [Fact]
  public void Suicide_and_team_kill_give_no_kill()
  {
    var ledger = new StatsLedger();

    Assert.Null(ledger.RecordDeath(Victim, Victim, []));
    Assert.Null(ledger.RecordDeath(Victim, Enemy, []));

    Assert.Equal(new PlayerStats(0, 2, 0), ledger.Get(Victim.SteamId));
    Assert.Equal(PlayerStats.Empty, ledger.Get(Enemy.SteamId));
  }

  [Fact]
  public void Assists_are_deduplicated_and_exclude_killer_victim_and_other_team()
  {
    var ledger = new StatsLedger();

    ledger.RecordDeath(Victim, Killer, [Helper, Helper, Killer, Victim, Enemy]);

    Assert.Equal(1, ledger.Get(Helper.SteamId).Assists);
    Assert.Equal(0, ledger.Get(Killer.SteamId).Assists);
    Assert.Equal(0, ledger.Get(Enemy.SteamId).Assists);
  }

  [Fact]
  public void Score_total_and_reset()
  {
    var ledger = new StatsLedger();

    ledger.RecordDeath(Victim, Killer, [Helper]);
    ledger.RecordDeath(Enemy, Killer, []);

    Assert.Equal(2, ledger.Get(Killer.SteamId).Score);
    Assert.Equal(-1, ledger.Get(Victim.SteamId).Score);
    Assert.Equal(new PlayerStats(2, 0, 1), ledger.Total([Killer.SteamId, Helper.SteamId]));
    Assert.Equal("2 / 0 / 0", ledger.Get(Killer.SteamId).Line);

    ledger.Reset();

    Assert.Equal(0, ledger.Count);
    Assert.Equal(PlayerStats.Empty, ledger.Get(Killer.SteamId));
  }
}
