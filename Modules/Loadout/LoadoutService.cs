using Bublock.Shared;
using DeadworksManaged.Api;
using ITimer = DeadworksManaged.Api.ITimer;

namespace Bublock.Modules.Loadout;

public sealed record LoadoutOptions(
  int Gold = 0,
  int Slots = LoadoutPlanner.DefaultSlots,
  int? MaxValue = null);

public sealed record LoadoutResult(
  int ItemsAdded,
  int ItemsFailed,
  int Imbued,
  int AbilitiesSet,
  int AbilitiesMissing,
  IReadOnlyList<string> Unknown,
  int Value,
  IReadOnlyList<string> ItemsCapped,
  ProgressionLevel Progression,
  AbilityPlan AbilityPlan);

public static class LoadoutService
{
  public const double SwapDelaySeconds = 1.0;

  public const int SwapAttempts = 3;

  public const int DefaultMaxValue = LoadoutPlanner.DefaultCap;

  private static readonly Logger Log = BublockLog.For("Loadout");

  public static int MaxValue { get; private set; } = DefaultMaxValue;

  public static void SetMaxValue(int souls, ExecutionMode mode = ExecutionMode.Clean)
  {
    var previous = MaxValue;
    MaxValue = souls;

    Log.WithMode(mode).Info("Loadout cap set Previous={Previous} Cap={Cap}", previous, souls);
    BublockLog.Master.Info("Loadout cap {Previous} -> {Cap}", previous, souls);
  }

  private static readonly EAbilitySlot[] SignatureSlots =
  [
    EAbilitySlot.Signature1,
    EAbilitySlot.Signature2,
    EAbilitySlot.Signature3,
    EAbilitySlot.Signature4
  ];

  public static LoadoutResult Apply(
    CCitadelPlayerPawn pawn,
    HeroBuild build,
    LoadoutOptions? options = null,
    HeroBuildCatalog? catalog = null,
    ExecutionMode mode = ExecutionMode.Clean,
    Random? rng = null)
  {
    options ??= new LoadoutOptions();
    catalog ??= HeroBuildCatalog.Default;
    rng ??= Random.Shared;

    var log = Log.WithMode(mode);
    var who = pawn.Controller?.ToPlayerRef();

    var unknown = new List<string>();
    var slots = LoadoutPlanner.FirstSlots(
      LoadoutPlanner.ItemOrder(build, rng),
      catalog.ComponentsOf,
      options.Slots,
      item =>
      {
        if (ItemInfo.Exists(item))
          return true;

        unknown.Add(item);
        return false;
      });

    var cap = options.MaxValue ?? MaxValue;
    var (items, capped) = LoadoutPlanner.CapValue(slots, catalog.CostOf, cap);
    var value = LoadoutPlanner.Value(items, catalog.CostOf);

    if (capped.Count > 0)
    {
      Info(
        log,
        who,
        "Loadout over cap, removed Removed={Removed} Value={Value} Cap={Cap} Baseline={Baseline}",
        string.Join(",", capped),
        value,
        cap,
        catalog.BaselineValue);
    }

    var progression = Progression.ForSouls(value);
    var plan = LoadoutPlanner.AbilityPrefix(build.Abilities ?? [], progression.Unlocks, progression.AbilityPoints);

    pawn.ResetHero();
    pawn.Level = progression.Level;
    pawn.ModifyCurrency(ECurrencyType.EGold, 0, ECurrencySource.ECheats, silent: true);

    var abilitiesSet = 0;
    var abilitiesMissing = 0;

    foreach (var (name, bits) in plan.Bits)
    {
      var ability = pawn.AbilityComponent.FindAbilityByName(name);

      if (ability == null)
      {
        abilitiesMissing++;
        Trace(log, who, "Ability not on hero Ability={Ability}", name);
        continue;
      }

      ability.UpgradeBits = bits;
      abilitiesSet++;
    }

    var added = 0;
    var failed = 0;
    var imbued = 0;

    foreach (var name in items)
    {
      var item = pawn.AddItem(name);

      if (item == null)
      {
        failed++;
        Trace(log, who, "AddItem failed Item={Item}", name);
        continue;
      }

      added++;

      if (ItemInfo.CanBeImbued(name) && Imbue(pawn, item, name, build))
        imbued++;
    }

    pawn.SetCurrency(ECurrencyType.EGold, options.Gold);
    pawn.SetCurrency(ECurrencyType.EAbilityPoints, 0);
    pawn.SetCurrency(ECurrencyType.EAbilityUnlocks, 0);
    pawn.Heal(pawn.GetMaxHealth());

    var result = new LoadoutResult(
      added, failed, imbued, abilitiesSet, abilitiesMissing, unknown, value, capped, progression, plan);

    Info(
      log,
      who,
      "Loadout applied Hero={Hero} Build={Build} BuildId={BuildId} Items={Items} Failed={Failed} Imbued={Imbued} " +
      "Abilities={Abilities} AbilitiesMissing={AbilitiesMissing} Unknown={Unknown} Value={Value} Baseline={Baseline} " +
      "Capped={Capped} Level={Level} Boons={Boons} Unlocks={Unlocks} Points={Points} PointsLeft={PointsLeft} " +
      "Steps={Steps} StepsTotal={StepsTotal} Ranks={Ranks} Gold={Gold}",
      pawn.HeroID, build.Name, build.BuildId, added, failed, imbued,
      abilitiesSet, abilitiesMissing, string.Join(",", unknown), value, catalog.BaselineValue,
      capped.Count, progression.Level, progression.Boons, progression.Unlocks, progression.AbilityPoints,
      progression.AbilityPoints - plan.PointsUsed, plan.StepsTaken, plan.StepsTotal,
      string.Join(",", plan.Bits.Select(entry => $"{entry.Ability}:{Convert.ToString(entry.Bits, 2)}")), options.Gold);

    if (unknown.Count > 0 || failed > 0)
      log.Warn("Loadout incomplete BuildId={BuildId} Failed={Failed} Unknown={Unknown}", build.BuildId, failed, string.Join(",", unknown));

    return result;
  }

  public static bool Swap(
    CCitadelPlayerController player,
    Heroes hero,
    HeroBuild build,
    ITimer timer,
    LoadoutOptions? options = null,
    ExecutionMode mode = ExecutionMode.Clean,
    Action<CCitadelPlayerController, LoadoutResult>? applied = null)
  {
    var log = Log.WithMode(mode);
    var pawn = player.GetHeroPawn();

    if (pawn == null || !pawn.IsAlive)
    {
      log.Debug(player.ToPlayerRef(), "Swap skipped, no live pawn Hero={Hero}", hero);
      return false;
    }

    player.SelectHero(hero);
    log.Debug(player.ToPlayerRef(), "Hero selected, loadout pending Hero={Hero} BuildId={BuildId}", hero, build.BuildId);

    AfterSwap(player.PlayerSteamId, hero, timer, log, (current, currentPawn) =>
      applied?.Invoke(current, Apply(currentPawn, build, options, mode: mode)));

    return true;
  }

  public static LoadoutSnapshot Capture(CCitadelPlayerPawn pawn, string source = "")
  {
    var abilities = new List<SnapshotAbility>();
    var items = new List<SnapshotItem>();

    foreach (var ability in pawn.AbilityComponent.Abilities)
    {
      if (ability.IsItem)
        items.Add(new SnapshotItem(ability.AbilityName, ability.ImbuedAbilities.ToList()));
      else if (ability.IsSignature)
        abilities.Add(new SnapshotAbility(ability.AbilityName, ability.AbilitySlot, ability.UpgradeBits));
    }

    return new LoadoutSnapshot(
      pawn.HeroID,
      pawn.Level,
      pawn.GetCurrency(ECurrencyType.EAbilityPoints),
      pawn.GetCurrency(ECurrencyType.EAbilityUnlocks),
      abilities,
      items,
      source);
  }

  public static SnapshotResult ApplySnapshot(
    CCitadelPlayerPawn pawn,
    LoadoutSnapshot snapshot,
    int gold = 0,
    ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var who = pawn.Controller?.ToPlayerRef();

    pawn.ResetHero();
    pawn.Level = snapshot.Level;
    pawn.ModifyCurrency(ECurrencyType.EGold, 0, ECurrencySource.ECheats, silent: true);

    var abilitiesSet = 0;
    var abilitiesMissing = 0;

    foreach (var ability in snapshot.Abilities)
    {
      var target = pawn.AbilityComponent.FindAbilityByName(ability.Name) ?? pawn.AbilityComponent.GetAbilityBySlot(ability.Slot);

      if (target == null)
      {
        abilitiesMissing++;
        Trace(log, who, "Snapshot ability not on hero Ability={Ability} Slot={Slot}", ability.Name, ability.Slot);
        continue;
      }

      target.UpgradeBits = ability.UpgradeBits;
      abilitiesSet++;
    }

    pawn.SetCurrency(ECurrencyType.EAbilityPoints, snapshot.AbilityPoints);
    pawn.SetCurrency(ECurrencyType.EAbilityUnlocks, snapshot.AbilityUnlocks);

    var added = 0;
    var failed = 0;
    var imbued = 0;

    foreach (var entry in snapshot.Items)
    {
      var item = pawn.AddItem(entry.Name);

      if (item == null)
      {
        failed++;
        Trace(log, who, "Snapshot AddItem failed Item={Item}", entry.Name);
        continue;
      }

      added++;

      foreach (var targetName in entry.ImbuedAbilities)
      {
        var target = pawn.AbilityComponent.FindAbilityByName(targetName);

        if (target != null && pawn.ImbueItem(item, target.AbilitySlot) == ImbueResult.Success)
          imbued++;
      }
    }

    pawn.SetCurrency(ECurrencyType.EGold, gold);
    pawn.Heal(pawn.GetMaxHealth());

    Info(
      log,
      who,
      "Snapshot applied Hero={Hero} Level={Level} AP={AP} Unlocks={Unlocks} Items={Items} Failed={Failed} Imbued={Imbued} " +
      "Abilities={Abilities} AbilitiesMissing={AbilitiesMissing} Gold={Gold} From={From}",
      snapshot.Hero, snapshot.Level, snapshot.AbilityPoints, snapshot.AbilityUnlocks, added, failed, imbued,
      abilitiesSet, abilitiesMissing, gold, snapshot.Source);

    if (failed > 0 || abilitiesMissing > 0)
      log.Warn("Snapshot incomplete Hero={Hero} Failed={Failed} AbilitiesMissing={AbilitiesMissing}", snapshot.Hero, failed, abilitiesMissing);

    return new SnapshotResult(added, failed, imbued, abilitiesSet, abilitiesMissing);
  }

  public static bool SwapSnapshot(
    CCitadelPlayerController player,
    LoadoutSnapshot snapshot,
    ITimer timer,
    int gold = 0,
    ExecutionMode mode = ExecutionMode.Clean,
    Action<CCitadelPlayerController, SnapshotResult>? applied = null)
  {
    var log = Log.WithMode(mode);
    var pawn = player.GetHeroPawn();

    if (pawn == null || !pawn.IsAlive)
    {
      log.Debug(player.ToPlayerRef(), "Snapshot swap skipped, no live pawn Hero={Hero}", snapshot.Hero);
      return false;
    }

    if (pawn.HeroID == snapshot.Hero)
    {
      applied?.Invoke(player, ApplySnapshot(pawn, snapshot, gold, mode));
      return true;
    }

    player.SelectHero(snapshot.Hero);
    log.Debug(player.ToPlayerRef(), "Hero selected, snapshot pending Hero={Hero}", snapshot.Hero);

    AfterSwap(player.PlayerSteamId, snapshot.Hero, timer, log, (current, currentPawn) =>
      applied?.Invoke(current, ApplySnapshot(currentPawn, snapshot, gold, mode)));

    return true;
  }

  private static void AfterSwap(
    ulong steamId,
    Heroes hero,
    ITimer timer,
    Logger log,
    Action<CCitadelPlayerController, CCitadelPlayerPawn> apply,
    int attempt = 1)
  {
    timer.Once(SwapDelaySeconds.Seconds(), () =>
    {
      var current = Players.GetAll().FirstOrDefault(candidate => candidate.PlayerSteamId == steamId);
      var currentPawn = current?.GetHeroPawn();

      if (current == null || currentPawn == null || !currentPawn.IsAlive)
      {
        log.Info("Loadout skipped, player gone or dead SteamId={SteamId} Hero={Hero}", steamId, hero);
        return;
      }

      if (currentPawn.HeroID != hero)
      {
        if (attempt >= SwapAttempts)
        {
          log.Warn(
            current.ToPlayerRef(),
            "Loadout skipped, hero did not change Expected={Expected} Current={Current} Attempts={Attempts}",
            hero,
            currentPawn.HeroID,
            attempt);
          return;
        }

        log.Info(
          current.ToPlayerRef(),
          "Hero did not change, selecting again Expected={Expected} Current={Current} Attempt={Attempt}",
          hero,
          currentPawn.HeroID,
          attempt + 1);
        current.SelectHero(hero);
        AfterSwap(steamId, hero, timer, log, apply, attempt + 1);
        return;
      }

      try
      {
        apply(current, currentPawn);
      }
      catch (Exception exception)
      {
        log.Error(current.ToPlayerRef(), exception, "Loadout failed Hero={Hero}", hero);
      }
    });
  }

  private static bool Imbue(CCitadelPlayerPawn pawn, CCitadelBaseAbility item, string name, HeroBuild build)
  {
    if (build.Imbues != null && build.Imbues.TryGetValue(name, out var targetName))
    {
      var target = pawn.AbilityComponent.FindAbilityByName(targetName);

      if (target != null && pawn.ImbueItem(item, target.AbilitySlot) == ImbueResult.Success)
        return true;
    }

    foreach (var slot in SignatureSlots)
    {
      if (pawn.CanImbue(name, slot) && pawn.ImbueItem(item, slot) == ImbueResult.Success)
        return true;
    }

    return false;
  }

  private static void Info(Logger log, PlayerRef? who, string template, params object?[] args)
  {
    if (who is { } player)
      log.Info(player, template, args);
    else
      log.Info(template, args);
  }

  private static void Trace(Logger log, PlayerRef? who, string template, params object?[] args)
  {
    if (who is { } player)
      log.Trace(player, template, args);
    else
      log.Trace(template, args);
  }
}
