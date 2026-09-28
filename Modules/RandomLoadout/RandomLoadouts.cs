using Bublock.Modules.Loadout;
using Bublock.Shared;
using DeadworksManaged.Api;
using ITimer = DeadworksManaged.Api.ITimer;

namespace Bublock.Modules.RandomLoadout;

public sealed record RandomPick(Heroes Hero, HeroBuild Build);

// Random hero + one of its stored top builds per player, given through Modules/Loadout.
// The game type owns one instance and decides when to draw (join, kill, timer).
public sealed class RandomLoadouts
{
  private readonly Dictionary<ulong, RandomPick> _picks = [];
  private readonly HashSet<ulong> _pending = [];
  private readonly Logger _log = BublockLog.For("RandomLoadout");
  private readonly Random _rng;

  public RandomLoadouts(HeroBuildCatalog catalog, LoadoutOptions options, Random rng)
  {
    Catalog = catalog;
    Options = options;
    _rng = rng;
  }

  public HeroBuildCatalog Catalog { get; }

  public LoadoutOptions Options { get; }

  public int PendingCount => _pending.Count;

  // Runs every time a loadout lands (the game type's banner).
  public Action<CCitadelPlayerController, RandomPick, LoadoutResult>? Landed { get; set; }

  public bool IsPending(ulong steamId) => _pending.Contains(steamId);

  public bool TryGet(ulong steamId, out RandomPick pick) => _picks.TryGetValue(steamId, out pick!);

  // A new hero (not the current one, preferring heroes nobody else holds) and a random build of it.
  public RandomPick? Draw(ulong steamId)
  {
    var current = _picks.TryGetValue(steamId, out var now) ? now.Hero : (Heroes?)null;
    var taken = _picks.Where(pair => pair.Key != steamId).Select(pair => pair.Value.Hero).ToHashSet();

    if (HeroRoll.Pick(Catalog.Heroes, current, taken, _rng) is not { } hero)
      return null;

    var builds = Catalog.BuildsFor(hero);
    var pick = new RandomPick(hero, builds[_rng.Next(builds.Count)]);
    _picks[steamId] = pick;
    return pick;
  }

  // Swaps the player to their current pick now, or marks it pending when they are dead.
  public bool Give(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;

    if (!_picks.TryGetValue(steamId, out var pick))
      return false;

    var started = LoadoutService.Swap(player, pick.Hero, pick.Build, timer, Options, mode, (current, result) =>
    {
      if (!_picks.TryGetValue(steamId, out var live) || live != pick)
        return;

      Landed?.Invoke(current, pick, result);
    });

    if (started)
      _pending.Remove(steamId);
    else
      _pending.Add(steamId);

    _log.WithMode(mode).Info(player.ToPlayerRef(), started ? "Loadout started Hero={Hero} Build={Build}" : "Loadout pending spawn Hero={Hero} Build={Build}", pick.Hero, pick.Build.Name);
    return started;
  }

  // Draw + Give: the whole "new random hero" step.
  public RandomPick? Roll(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (Draw(player.PlayerSteamId) is not { } pick)
      return null;

    Give(player, timer, mode);
    return pick;
  }

  // A joiner whose hero was just selected by the game type: the build waits for their spawn.
  public bool Hold(ulong steamId) => _picks.ContainsKey(steamId) && _pending.Add(steamId);

  // From a spawn hook (next tick): a pending player gets their pick now that they are alive.
  public bool ApplyPending(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean) =>
    _pending.Contains(player.PlayerSteamId) && Give(player, timer, mode);

  public void Forget(ulong steamId)
  {
    _picks.Remove(steamId);
    _pending.Remove(steamId);
  }

  public void Clear()
  {
    _picks.Clear();
    _pending.Clear();
  }

  public IReadOnlyList<string> Describe(Func<ulong, string> nameOf) =>
    _picks.Count == 0
      ? ["No heroes given yet."]
      : _picks.Select(pair => $"{nameOf(pair.Key)}: {Catalog.DisplayName(pair.Value.Hero)} ({pair.Value.Build.Name}){(_pending.Contains(pair.Key) ? " PENDING" : "")}").ToList();
}
