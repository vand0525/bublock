using DeadworksManaged.Api;
using RiftRoulette.Draft;

namespace Bublock.Tests.RiftRoulette;

public class DraftStateTests
{
  private const ulong Theo = 76561198000000001;
  private const ulong Other = 76561198000000002;

  public DraftStateTests()
  {
    DraftState.Clear();
  }

  [Fact]
  public void Add_records_pick_and_marks_hero_selected()
  {
    DraftState.Add(Theo, Heroes.Shiv);

    Assert.True(DraftState.HasPick(Theo));
    Assert.True(DraftState.TryGetPick(Theo, out var hero));
    Assert.Equal(Heroes.Shiv, hero);
    Assert.True(DraftState.IsSelected(Heroes.Shiv));
    Assert.Equal([Heroes.Shiv], DraftState.SelectedHeroes);
  }

  [Fact]
  public void Release_removes_pick_and_frees_hero()
  {
    DraftState.Add(Theo, Heroes.Shiv);
    DraftState.Add(Other, Heroes.Fencer);

    Assert.True(DraftState.Release(Theo, out var hero));
    Assert.Equal(Heroes.Shiv, hero);
    Assert.False(DraftState.HasPick(Theo));
    Assert.False(DraftState.IsSelected(Heroes.Shiv));
    Assert.True(DraftState.IsSelected(Heroes.Fencer));
  }

  [Fact]
  public void Release_without_pick_returns_false()
  {
    Assert.False(DraftState.Release(Theo, out _));
    Assert.False(DraftState.TryGetPick(Theo, out _));
  }

  [Fact]
  public void Second_pick_for_same_player_throws()
  {
    DraftState.Add(Theo, Heroes.Shiv);

    Assert.Throws<ArgumentException>(() => DraftState.Add(Theo, Heroes.Yamato));
  }

  [Fact]
  public void Clear_empties_everything()
  {
    DraftState.Add(Theo, Heroes.Shiv);
    DraftState.Add(Other, Heroes.Fencer);

    DraftState.Clear();

    Assert.Empty(DraftState.SelectedHeroes);
    Assert.False(DraftState.HasPick(Theo));
    Assert.False(DraftState.HasPick(Other));
  }
}
