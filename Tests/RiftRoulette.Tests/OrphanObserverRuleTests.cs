using RiftRoulette.Lobby;

namespace Bublock.Tests.RiftRoulette;

public class OrphanObserverRuleTests
{
  private const uint None = OrphanObserverRule.NoHandle;

  private static uint Controller(int slot, uint serial = 1) => (uint)(slot + 1) | (serial << 15);

  [Fact]
  public void SlotOf_reads_the_controller_index()
  {
    Assert.Equal(3, OrphanObserverRule.SlotOf(Controller(3, 7)));
    Assert.Equal(-1, OrphanObserverRule.SlotOf(None));
  }

  [Fact]
  public void Removes_the_leavers_observer()
  {
    var leaver = Controller(4);
    Assert.True(OrphanObserverRule.ShouldRemove(leaver, 4, leaver, None, slotConnected: false));
  }

  [Fact]
  public void Keeps_observers_of_other_slots()
  {
    var admin = Controller(0);
    Assert.False(OrphanObserverRule.ShouldRemove(admin, 4, Controller(4), None, slotConnected: false));
    Assert.False(OrphanObserverRule.ShouldRemove(admin, 4, None, None, slotConnected: false));
  }

  [Fact]
  public void Keeps_unowned_observers()
  {
    Assert.False(OrphanObserverRule.ShouldRemove(None, 4, Controller(4), None, slotConnected: false));
  }

  [Fact]
  public void Keeps_everything_once_the_slot_is_connected_again()
  {
    var leaver = Controller(4);
    Assert.False(OrphanObserverRule.ShouldRemove(leaver, 4, leaver, None, slotConnected: true));
  }

  [Fact]
  public void Keeps_a_new_controllers_observer_in_the_same_slot()
  {
    var joiner = Controller(4, serial: 2);
    Assert.False(OrphanObserverRule.ShouldRemove(joiner, 4, Controller(4), joiner, slotConnected: false));
  }

  [Fact]
  public void Without_the_leavers_handle_removes_unless_the_slot_controller_owns_it()
  {
    var leftover = Controller(4);
    var joiner = Controller(4, serial: 2);
    Assert.True(OrphanObserverRule.ShouldRemove(leftover, 4, None, None, slotConnected: false));
    Assert.True(OrphanObserverRule.ShouldRemove(leftover, 4, None, joiner, slotConnected: false));
    Assert.False(OrphanObserverRule.ShouldRemove(joiner, 4, None, joiner, slotConnected: false));
  }
}
