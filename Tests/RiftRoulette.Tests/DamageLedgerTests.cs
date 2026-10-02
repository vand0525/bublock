using RiftRoulette.Stats;

namespace Bublock.Tests.RiftRoulette;

public class DamageLedgerTests
{
  private const ulong Victim = 1;
  private const ulong Killer = 2;
  private const ulong Helper = 3;
  private const ulong Other = 4;
  private const int MaxHealth = 1000;

  [Fact]
  public void Exactly_the_fraction_of_max_health_earns_an_assist()
  {
    var ledger = new DamageLedger();
    ledger.Record(Victim, Helper, 200);

    Assert.Equal([Helper], ledger.Assisters(Victim, Killer, MaxHealth));
  }

  [Fact]
  public void Just_below_the_fraction_earns_nothing()
  {
    var ledger = new DamageLedger();
    ledger.Record(Victim, Helper, 199.9);

    Assert.Empty(ledger.Assisters(Victim, Killer, MaxHealth));
  }

  [Fact]
  public void Hits_add_up()
  {
    var ledger = new DamageLedger();
    ledger.Record(Victim, Helper, 120);
    ledger.Record(Victim, Helper, 90);

    Assert.Equal(210, ledger.DamageTo(Victim)[Helper]);
    Assert.Equal([Helper], ledger.Assisters(Victim, Killer, MaxHealth));
  }

  [Fact]
  public void Killer_is_never_an_assister()
  {
    var ledger = new DamageLedger();
    ledger.Record(Victim, Killer, 900);
    ledger.Record(Victim, Helper, 300);

    Assert.Equal([Helper], ledger.Assisters(Victim, Killer, MaxHealth));
  }

  [Fact]
  public void Most_damage_comes_first()
  {
    var ledger = new DamageLedger();
    ledger.Record(Victim, Helper, 250);
    ledger.Record(Victim, Other, 400);

    Assert.Equal([Other, Helper], ledger.Assisters(Victim, Killer, MaxHealth));
  }

  [Fact]
  public void Self_damage_zero_ids_and_non_positive_amounts_are_ignored()
  {
    var ledger = new DamageLedger();
    ledger.Record(Victim, Victim, 500);
    ledger.Record(Victim, 0, 500);
    ledger.Record(0, Helper, 500);
    ledger.Record(Victim, Helper, 0);
    ledger.Record(Victim, Helper, -50);

    Assert.Empty(ledger.DamageTo(Victim));
  }

  [Fact]
  public void Unknown_max_health_gives_no_assists()
  {
    var ledger = new DamageLedger();
    ledger.Record(Victim, Helper, 500);

    Assert.Empty(ledger.Assisters(Victim, Killer, 0));
  }

  [Fact]
  public void Clear_drops_only_that_victim()
  {
    var ledger = new DamageLedger();
    ledger.Record(Victim, Helper, 500);
    ledger.Record(Other, Helper, 500);

    ledger.Clear(Victim);

    Assert.Empty(ledger.DamageTo(Victim));
    Assert.Single(ledger.DamageTo(Other));
  }

  [Fact]
  public void Forget_drops_the_player_as_victim_and_attacker()
  {
    var ledger = new DamageLedger();
    ledger.Record(Victim, Helper, 500);
    ledger.Record(Helper, Victim, 500);
    ledger.Record(Other, Helper, 500);
    ledger.Record(Other, Killer, 500);

    ledger.Forget(Helper);

    Assert.Empty(ledger.DamageTo(Helper));
    Assert.Empty(ledger.DamageTo(Victim));
    Assert.Equal([Killer], ledger.DamageTo(Other).Keys);
  }

  [Fact]
  public void Reset_clears_everything()
  {
    var ledger = new DamageLedger();
    ledger.Record(Victim, Helper, 500);
    ledger.Record(Other, Killer, 500);

    ledger.Reset();

    Assert.Empty(ledger.DamageTo(Victim));
    Assert.Empty(ledger.DamageTo(Other));
  }

  [Fact]
  public void Custom_fraction_is_respected()
  {
    var ledger = new DamageLedger();
    ledger.Record(Victim, Helper, 100);

    Assert.Empty(ledger.Assisters(Victim, Killer, MaxHealth));
    Assert.Equal([Helper], ledger.Assisters(Victim, Killer, MaxHealth, fraction: 0.1));
  }
}
