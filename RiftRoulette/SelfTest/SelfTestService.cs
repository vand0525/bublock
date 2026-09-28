using System.Numerics;
using System.Reflection;
using Bublock.Modules.Hud;
using Bublock.Modules.Loadout;
using Bublock.Modules.Movement;
using Bublock.Modules.Restraint;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Lobby;
using RiftRoulette.Locations;
using RiftRoulette.Rift;
using RiftRoulette.Round;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.SelfTest;

public static class SelfTestService
{
  public const float LandingTolerance = 32f;

  public const float FloorProbeUp = 32f;

  public const float FloorProbeDown = 96f;

  public const int BuildDataMaxAgeDays = 14;

  public const double LiveCheckSeconds = 1.0;

  private const MaskTrace FloorMask = MaskTrace.Solid | MaskTrace.PlayerClip | MaskTrace.WorldGeometry;

  private static readonly Logger Log = BublockLog.For("SelfTest");

  public static IReadOnlyList<CheckResult> Run(ExecutionMode mode = ExecutionMode.Clean)
  {
    var results = new List<CheckResult>();

    Guard(results, "Convars", CheckConVars);
    Guard(results, "Schema", CheckSchema);
    Guard(results, "Entities", CheckEntities);
    Guard(results, "RiftPoints", CheckRiftPoints);
    Guard(results, "Heroes", CheckHeroes);
    Guard(results, "Items", CheckItems);
    Guard(results, "Floor", CheckFloors);
    Guard(results, "Players", CheckPlayers);
    Guard(results, "Events", CheckEvents);

    Record(results, "Self-test", mode);
    return results;
  }

  public static string Live(CCitadelPlayerController? caller, CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (RiftService.IsRunning)
      return "A round is running; run selftest_live between rounds.";

    var pawn = player.GetHeroPawn();

    if (pawn == null || !pawn.IsAlive)
      return $"Slot {player.Slot} has no living hero.";

    var steamId = player.PlayerSteamId;
    var wasRestrained = RestraintService.IsRestrained(steamId);
    var target = SlotSpots.Watch(WatchSpot.Location(WatchSpot.Side), player.Slot);
    var results = new List<CheckResult>();

    Guard(results, "Loadout", () => CheckLoadout(pawn));
    Guard(results, "Hud", () =>
    {
      HudService.Announce(player, "Self-test", "Banner check", mode);
      return [new CheckResult("Hud", "banner", CheckStatus.Pass, "sent; confirm it on screen")];
    });

    WatchSpot.SendUp(player, mode);
    Log.WithMode(mode).Info(player.ToPlayerRef(), "Live self-test started Target={Target} WasRestrained={WasRestrained}", target.Position, wasRestrained);

    timer.Once(LiveCheckSeconds.Seconds(), () =>
    {
      var current = Players.GetAll().FirstOrDefault(other => other.PlayerSteamId == steamId);

      if (current == null)
      {
        Log.WithMode(mode).Warn("Live self-test player left SteamId={SteamId}", steamId);
        return;
      }

      Guard(results, "Teleport", () => CheckLanding(current, target));
      Guard(results, "Restraint", () => CheckRestraint(current));

      if (!wasRestrained)
        RestraintService.Release(current, mode);

      Record(results, "Live self-test", mode);

      foreach (var line in SelfTestReport.Lines(results, all: true))
        AdminCommand.Reply(caller, $"[SelfTest] {line}");
    });

    return $"Live self-test on slot {player.Slot} ({player.PlayerName}); results in {LiveCheckSeconds:0.#} s.";
  }

  private static IEnumerable<CheckResult> CheckConVars()
  {
    foreach (var dependency in GameDependencies.ConVars)
    {
      var convar = ConVar.Find(dependency.Name);

      if (convar == null)
      {
        yield return new CheckResult("Convars", dependency.Name, dependency.IfMissing, $"not found ({dependency.Owner})");
        continue;
      }

      var value = convar.GetInt();

      yield return dependency.Expected is { } expected && value != expected
        ? new CheckResult("Convars", dependency.Name, CheckStatus.Warn, $"value {value}, expected {expected} ({dependency.Owner})")
        : new CheckResult("Convars", dependency.Name, CheckStatus.Pass, $"value {value}");
    }
  }

  private static IEnumerable<CheckResult> CheckSchema()
  {
    var fields = new (string Name, object Accessor)[]
    {
      ("CCitadelGameRulesProxy.m_pGameRules", new SchemaAccessor<nint>("CCitadelGameRulesProxy"u8, "m_pGameRules"u8)),
      ("CCitadelGameRules.m_vNextKothLocation", RiftGameRules.NextLocation),
      ("CCitadelGameRules.m_timeNextKothSpawnWindowTime", RiftGameRules.NextWindow),
      ("CCitadelGameRules.m_timeNextKothSpawn", RiftGameRules.NextSpawn),
      ("CCitadelGameRules.m_timeKothGiveUp", RiftGameRules.KothGiveUp),
      ("CCitadelTeam.m_nFlexSlotsUnlocked", FlexSlots.Unlocked)
    };

    var allFound = true;

    foreach (var (name, accessor) in fields)
    {
      var offset = OffsetOf(accessor);

      if (offset is not { } found)
      {
        allFound = false;
        yield return new CheckResult("Schema", name, CheckStatus.Warn, "offset not readable (Deadworks SchemaAccessor changed)");
      }
      else if (found <= 0)
      {
        allFound = false;
        yield return new CheckResult("Schema", name, CheckStatus.Fail, $"field not found (offset {found})");
      }
      else
      {
        yield return new CheckResult("Schema", name, CheckStatus.Pass, $"offset {found}");
      }
    }

    var resolve = RiftGameRules.TryResolve(out var gameRules);

    if (resolve != RiftGameRules.ResolveResult.Found)
    {
      yield return new CheckResult("Schema", "gamerules pointer", CheckStatus.Fail, resolve.ToString());
      yield break;
    }

    if (!allFound)
      yield break;

    var nextSpawn = RiftGameRules.NextSpawn.Get(gameRules);
    var giveUp = RiftGameRules.KothGiveUp.Get(gameRules);

    yield return float.IsFinite(nextSpawn) && float.IsFinite(giveUp)
      ? new CheckResult("Schema", "KOTH timers", CheckStatus.Pass, $"NextSpawn={nextSpawn:0.#} GiveUp={giveUp:0.#}")
      : new CheckResult("Schema", "KOTH timers", CheckStatus.Fail, $"unreadable values NextSpawn={nextSpawn} GiveUp={giveUp}");
  }

  private static IEnumerable<CheckResult> CheckEntities()
  {
    foreach (var name in GameDependencies.RequiredEntities)
    {
      var count = Entities.ByDesignerName(name).Count();
      yield return new CheckResult("Entities", name, count > 0 ? CheckStatus.Pass : CheckStatus.Fail, $"{count} on the map");
    }

    foreach (var name in GameDependencies.KeptEntities)
    {
      var count = Entities.ByDesignerName(name).Count();
      yield return count > 0
        ? new CheckResult("Entities", name, CheckStatus.Pass, $"{count} on the map")
        : new CheckResult("Entities", name, CheckStatus.Warn, "none on the map (renamed, or not a server entity)");
    }

    foreach (var name in GameDependencies.CleanedEntities)
    {
      var count = Entities.ByDesignerName(name).Count();
      yield return count == 0
        ? new CheckResult("Entities", name, CheckStatus.Pass, "0 on the map")
        : new CheckResult("Entities", name, CheckStatus.Warn, $"{count} still on the map (CleanSlate missed it)");
    }
  }

  private static IEnumerable<CheckResult> CheckRiftPoints()
  {
    var points = Entities.ByDesignerName(GameDependencies.RiftPointName).Select(entity => entity.Position).ToList();

    if (points.Count == 0)
    {
      yield return new CheckResult("RiftPoints", GameDependencies.RiftPointName, CheckStatus.Warn, "none found; rift positions not confirmed");
      yield break;
    }

    foreach (var side in new[] { RiftSide.Green, RiftSide.Yellow })
    {
      var expected = RiftSides.Position(side);
      var nearest = points.MinBy(point => Vector3.Distance(point, expected));
      var distance = Vector3.Distance(nearest, expected);

      yield return distance <= GameDependencies.RiftPointTolerance
        ? new CheckResult("RiftPoints", RiftSides.Name(side), CheckStatus.Pass, $"map point {distance:0} units away")
        : new CheckResult("RiftPoints", RiftSides.Name(side), CheckStatus.Warn, $"nearest map point {nearest} is {distance:0} units away; update RiftSide");
    }
  }

  private static IEnumerable<CheckResult> CheckHeroes()
  {
    var catalog = HeroBuildCatalog.Default;

    yield return catalog.SkippedHeroIds.Count == 0
      ? new CheckResult("Heroes", "random pool", CheckStatus.Pass, $"{catalog.Heroes.Count} heroes")
      : new CheckResult("Heroes", "random pool", CheckStatus.Warn,
        $"{catalog.Heroes.Count} heroes; skipped ids {string.Join(",", catalog.SkippedHeroIds)} (not in the Heroes enum or no builds)");

    var age = DateTime.TryParse(catalog.Data.FetchedAt, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var fetched)
      ? DateTime.UtcNow - fetched
      : (TimeSpan?)null;

    yield return age is { } known && known.TotalDays <= BuildDataMaxAgeDays
      ? new CheckResult("Heroes", "build data age", CheckStatus.Pass, $"{known.TotalDays:0} days")
      : new CheckResult("Heroes", "build data age", CheckStatus.Warn, $"fetched {catalog.Data.FetchedAt}; rerun fetch-builds.py after a patch");
  }

  private static IEnumerable<CheckResult> CheckItems()
  {
    var catalog = HeroBuildCatalog.Default;
    var names = CatalogItems(catalog);
    var missing = names.Where(name => !ItemInfo.Exists(name)).ToList();

    yield return missing.Count == 0
      ? new CheckResult("Items", "build items", CheckStatus.Pass, $"{names.Count} items exist")
      : new CheckResult("Items", "build items", CheckStatus.Fail,
        $"{missing.Count}/{names.Count} missing: {string.Join(",", missing.Take(8))}{(missing.Count > 8 ? ",..." : "")}");

    foreach (var banned in LoadoutPlanner.Banned.Where(name => !ItemInfo.Exists(name)))
      yield return new CheckResult("Items", banned, CheckStatus.Warn, "banned item no longer exists (the ban does nothing)");
  }

  private static IEnumerable<CheckResult> CheckFloors()
  {
    var groups = new (MovementLocation Anchor, bool Watch)[]
    {
      (RiftRouletteLocations.WatchGreen, true),
      (RiftRouletteLocations.WatchYellow, true),
      (RiftRouletteLocations.GreenSapphire, false),
      (RiftRouletteLocations.GreenAmber, false),
      (RiftRouletteLocations.YellowSapphire, false),
      (RiftRouletteLocations.YellowAmber, false)
    };

    foreach (var (anchor, watch) in groups)
    {
      var count = watch ? SlotSpots.Offsets.Watch.Count : SlotSpots.Offsets.Fight.Count;
      var bare = Enumerable.Range(0, count)
        .Where(slot => !HasFloor((watch ? SlotSpots.Watch(anchor, slot) : SlotSpots.Fight(anchor, slot)).Position))
        .ToList();

      yield return bare.Count == 0
        ? new CheckResult("Floor", anchor.Name, CheckStatus.Pass, $"{count}/{count} spots have floor")
        : new CheckResult("Floor", anchor.Name, CheckStatus.Warn,
          $"{count - bare.Count}/{count} spots have floor; none under slots {string.Join(",", bare)}");
    }
  }

  private static IEnumerable<CheckResult> CheckPlayers()
  {
    var all = Players.GetAll().ToList();
    var humans = Participants.Humans();

    yield return new CheckResult("Players", "connected", CheckStatus.Pass, $"{all.Count} connected, {humans.Count} playing humans");

    foreach (var player in humans)
    {
      var name = $"slot {player.Slot} {player.PlayerName}";
      var pawn = player.GetHeroPawn();

      if (player.PlayerSteamId == 0)
        yield return new CheckResult("Players", name, CheckStatus.Fail, "SteamID reads 0 (controller schema changed)");
      else if (pawn == null)
        yield return new CheckResult("Players", name, CheckStatus.Warn, "no hero pawn");
      else if (!Enum.IsDefined(pawn.HeroID))
        yield return new CheckResult("Players", name, CheckStatus.Warn, $"hero id {(int)pawn.HeroID} is not in the Heroes enum");
      else
        yield return new CheckResult("Players", name, CheckStatus.Pass, $"{pawn.HeroID} team {RiftRouletteTeams.Name(player.TeamNum)} alive={pawn.IsAlive}");
    }
  }

  private static IEnumerable<CheckResult> CheckEvents()
  {
    foreach (var name in GameDependencies.Events)
    {
      var count = EventCounters.Count(name);

      yield return count > 0
        ? new CheckResult("Events", name, CheckStatus.Pass, $"{count} since load")
        : new CheckResult("Events", name, CheckStatus.Warn, "none since load (normal if it has not happened since the last upload)");
    }
  }

  private static IEnumerable<CheckResult> CheckLoadout(CCitadelPlayerPawn pawn)
  {
    var snapshot = LoadoutService.Capture(pawn, "selftest");

    yield return snapshot.Abilities.Count > 0
      ? new CheckResult("Loadout", "read hero", CheckStatus.Pass, snapshot.Describe())
      : new CheckResult("Loadout", "read hero", CheckStatus.Fail, $"no abilities read ({snapshot.Describe()})");
  }

  private static IEnumerable<CheckResult> CheckLanding(CCitadelPlayerController player, MovementLocation target)
  {
    var pawn = player.GetHeroPawn();

    if (pawn == null)
    {
      yield return new CheckResult("Teleport", target.Name, CheckStatus.Fail, "no pawn after teleport");
      yield break;
    }

    var distance = Vector3.Distance(pawn.Position, target.Position);

    yield return distance <= LandingTolerance
      ? new CheckResult("Teleport", target.Name, CheckStatus.Pass, $"landed {distance:0} units from the spot")
      : new CheckResult("Teleport", target.Name, CheckStatus.Fail, $"pawn at {pawn.Position}, {distance:0} units from the spot");
  }

  private static IEnumerable<CheckResult> CheckRestraint(CCitadelPlayerController player)
  {
    var states = player.GetHeroPawn()?.ModifierProp;

    if (states == null)
    {
      yield return new CheckResult("Restraint", "modifier prop", CheckStatus.Fail, "pawn has no ModifierProp");
      yield break;
    }

    foreach (var modifier in RestraintService.Modifiers)
      yield return new CheckResult("Restraint", modifier, states.HasModifier(modifier) ? CheckStatus.Pass : CheckStatus.Fail);

    foreach (var state in RestraintService.States)
      yield return new CheckResult("Restraint", state.ToString(), states.HasModifierState(state) ? CheckStatus.Pass : CheckStatus.Fail);
  }

  private static bool HasFloor(Vector3 position) =>
    Trace.Ray(position + Vector3.UnitZ * FloorProbeUp, position - Vector3.UnitZ * FloorProbeDown, FloorMask).DidHit;

  private static int? OffsetOf(object accessor) =>
    accessor.GetType().GetProperty("Offset", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(accessor) as int?;

  private static List<string> CatalogItems(HeroBuildCatalog catalog)
  {
    var names = new HashSet<string>();

    foreach (var set in catalog.Data.Heroes ?? [])
    foreach (var build in set.Builds ?? [])
    {
      names.UnionWith(build.Items ?? []);
      names.UnionWith((build.Categories ?? []).SelectMany(category => category.Items ?? []));
      names.UnionWith((build.Imbues ?? new Dictionary<string, string>()).Keys);
    }

    foreach (var (item, parts) in catalog.Data.Components ?? new Dictionary<string, IReadOnlyList<string>>())
    {
      names.Add(item);
      names.UnionWith(parts);
    }

    return names.Order().ToList();
  }

  private static void Guard(List<CheckResult> results, string area, Func<IEnumerable<CheckResult>> check)
  {
    try
    {
      results.AddRange(check());
    }
    catch (Exception exception)
    {
      results.Add(new CheckResult(area, "check threw", CheckStatus.Fail, $"{exception.GetType().Name}: {exception.Message}"));
      Log.Error(exception, "Self-test check threw Area={Area}", area);
    }
  }

  private static void Record(IReadOnlyCollection<CheckResult> results, string run, ExecutionMode mode)
  {
    var log = Log.WithMode(mode);

    foreach (var result in results)
    {
      switch (result.Status)
      {
        case CheckStatus.Pass:
          log.Info("PASS Area={Area} Check={Check} Detail={Detail}", result.Area, result.Name, result.Detail);
          break;
        case CheckStatus.Warn:
          log.Warn("WARN Area={Area} Check={Check} Detail={Detail}", result.Area, result.Name, result.Detail);
          break;
        default:
          log.Error("FAIL Area={Area} Check={Check} Detail={Detail}", result.Area, result.Name, result.Detail);
          break;
      }
    }

    BublockLog.Master.Info("{Run} finished {Summary}", run, SelfTestReport.Summarize(results));
  }
}
