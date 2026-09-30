using Bublock.Shared;
using DeadworksManaged.Api;

namespace Bublock.Modules.Loadout;

// #region agent log
public static class LoadoutStress
{
  public static bool Running;
}
// #endregion

public class LoadoutPlugin : DeadworksPluginBase
{
  private static readonly Logger CommandsLog = BublockLog.For("Loadout");

  public override string Name => "Loadout";

  // #region agent log
  private const double StressStepSeconds = 2.5;
  private const int StressCheckpointEvery = 10;
  // One cast step: swap (~1 s), 8 presses 1.5 s apart, then the after checkpoint.
  private const double CastStepSeconds = 16;
  private const double CastGapSeconds = 1.5;
  private static readonly EAbilitySlot[] CastSlots =
    [EAbilitySlot.Signature1, EAbilitySlot.Signature2, EAbilitySlot.Signature3, EAbilitySlot.Signature4];

  // Temporary probe: repeats hero swaps and builds on one player so string table growth per swap can be measured.
  // hero "all": swap goes through every hero once per pass, apply through every build of every hero; count is the number of passes.
  // how "cast": one step per hero (first build), then every signature ability cast twice, checkpoints before and after the casts.
  [Command("loadout_stress", Hidden = true, Description = "Probe: repeat swaps on a player: loadout_stress <slot> <count> [hero|random|all] [swap|apply|cast]")]
  public void CmdStress(CCitadelPlayerController? caller, int slot, int count, string hero = "random", string how = "swap")
  {
    AdminCommand.Authorize(caller, CommandsLog, "loadout_stress");

    var catalog = HeroBuildCatalog.Default;
    var player = Players.GetAll().FirstOrDefault(candidate => candidate.Slot == slot)
      ?? throw new CommandException($"No player in slot {slot}.");

    if (how != "swap" && how != "apply" && how != "cast")
      throw new CommandException("Mode must be swap, apply or cast.");

    var cast = how == "cast";
    var steps = new List<(Heroes Hero, HeroBuild Build, bool Swap)>();
    var withBuilds = catalog.Heroes.Where(candidate => catalog.BuildsFor(candidate).Count > 0).ToList();

    if (hero == "all")
    {
      for (var pass = 0; pass < Math.Max(1, count); pass++)
      {
        foreach (var candidate in withBuilds)
        {
          var builds = catalog.BuildsFor(candidate);

          for (var b = 0; b < (cast ? 1 : builds.Count); b++)
            steps.Add((candidate, builds[b], b == 0));
        }
      }
    }
    else if (hero == "random")
    {
      for (var i = 0; i < count; i++)
      {
        var pick = withBuilds[Random.Shared.Next(withBuilds.Count)];
        var builds = catalog.BuildsFor(pick);
        steps.Add((pick, builds[Random.Shared.Next(builds.Count)], how != "apply"));
      }
    }
    else
    {
      if (!catalog.TryParseHero(hero, out var parsed) || catalog.BuildsFor(parsed).Count == 0)
        throw new CommandException($"Unknown hero or no builds: '{hero}'.");

      // Cast mode on one hero swaps once, then recasts every step (does casting the same ability again add rows?).
      for (var i = 0; i < count; i++)
        steps.Add((parsed, catalog.BuildsFor(parsed)[0], how == "swap" || (cast && i == 0)));
    }

    var steamId = player.PlayerSteamId;
    var total = steps.Count;
    var stepSeconds = cast ? CastStepSeconds : StressStepSeconds;
    LoadoutStress.Running = true;
    StressCheckpoint(0, total, hero, how);

    for (var i = 0; i < total; i++)
    {
      var step = i + 1;
      var (pick, build, swap) = steps[i];

      Timer.Once((step * stepSeconds).Seconds(), () =>
      {
        var current = Players.GetAll().FirstOrDefault(candidate => candidate.PlayerSteamId == steamId);
        var pawn = current?.GetHeroPawn();

        if (current == null || pawn == null || !pawn.IsAlive)
        {
          CommandsLog.Warn("[agent] Stress step skipped, no live pawn Step={Step}", step);
        }
        else if (cast && swap)
        {
          LoadoutService.Swap(current, pick, build, Timer, applied: (_, _) => StressCast(steamId, pick, step, total));
        }
        else if (cast)
        {
          StressCast(steamId, pick, step, total);
        }
        else if (swap)
        {
          LoadoutService.Swap(current, pick, build, Timer);
        }
        else
        {
          LoadoutService.Apply(pawn, build);
        }

        if (!cast && (step % StressCheckpointEvery == 0 || step == total))
          Timer.Once((LoadoutService.SwapDelaySeconds + 0.5).Seconds(), () => StressCheckpoint(step, total, hero, how));

        if (step == total)
          Timer.Once((cast ? CastStepSeconds : LoadoutService.SwapDelaySeconds * LoadoutService.SwapAttempts + 1).Seconds(), () => LoadoutStress.Running = false);
      });
    }

    var dumps = cast ? "tables dump before and after each hero's casts" : $"tables dump every {StressCheckpointEvery}";
    AdminCommand.Reply(caller, $"[Loadout] Stress: {total} x {how} on {player.PlayerName} ({hero}), one every {stepSeconds}s (~{total * stepSeconds / 60:0.#} min); {dumps}.");
  }

  // Resets cooldowns before every press; the second round of presses fires recasts and ends toggles.
  private void StressCast(ulong steamId, Heroes hero, int step, int total)
  {
    StressCheckpoint(step, total, hero.ToString(), "cast-before");
    var target = Players.GetAll().FirstOrDefault(candidate => candidate.PlayerSteamId == steamId);

    if (target != null && Bublock.Modules.Restraint.RestraintService.Release(target))
      CommandsLog.Info(target.ToPlayerRef(), "[agent] Stress released restraint so casts are not silenced Step={Step}", step);

    var presses = CastSlots.Concat(CastSlots).ToArray();

    for (var i = 0; i < presses.Length; i++)
    {
      var slot = presses[i];
      var press = i + 1;

      Timer.Once((press * CastGapSeconds).Seconds(), () =>
      {
        var current = Players.GetAll().FirstOrDefault(candidate => candidate.PlayerSteamId == steamId);
        var pawn = current?.GetHeroPawn();

        if (current == null || pawn == null || !pawn.IsAlive || pawn.HeroID != hero)
        {
          CommandsLog.Warn("[agent] Stress cast skipped Step={Step} Hero={Hero} Slot={Slot} Current={Current}", step, hero, slot, pawn?.HeroID);
          return;
        }

        pawn.ResetAllAbilityCooldowns();
        var result = pawn.ExecuteAbilityBySlot(slot);
        CommandsLog.Info(current.ToPlayerRef(), "[agent] Stress cast Step={Step} Hero={Hero} Slot={Slot} Press={Press} Result={Result}", step, hero, slot, press, result);
      });
    }

    Timer.Once(((presses.Length + 1) * CastGapSeconds).Seconds(), () => StressCheckpoint(step, total, hero.ToString(), "cast-after"));
  }

  private static void StressCheckpoint(int step, int count, string hero, string how)
  {
    CommandsLog.Info("[agent] Stress checkpoint Step={Step} Of={Count} Hero={Hero} How={How} Running={Running}", step, count, hero, how, LoadoutStress.Running);
    Server.ExecuteCommand("dumpstringtable all sv simple");
  }
  // #endregion

  [Command("loadout_give", Description = "Swap a player to a hero and apply a top build: loadout_give <slot> <hero> [build 1-3]")]
  public void CmdGive(CCitadelPlayerController? caller, int slot, string hero, int build = 0)
  {
    AdminCommand.Authorize(caller, CommandsLog, "loadout_give");

    var catalog = HeroBuildCatalog.Default;
    var player = Players.GetAll().FirstOrDefault(candidate => candidate.Slot == slot)
      ?? throw new CommandException($"No player in slot {slot}.");

    if (!catalog.TryParseHero(hero, out var parsed))
      throw new CommandException($"Unknown hero '{hero}'.");

    var builds = catalog.BuildsFor(parsed);

    if (builds.Count == 0)
      throw new CommandException($"No builds for {catalog.DisplayName(parsed)}.");

    if (build < 0 || build > builds.Count)
      throw new CommandException($"Build must be 1-{builds.Count} (0 = random).");

    var chosen = builds[build == 0 ? Random.Shared.Next(builds.Count) : build - 1];

    if (!LoadoutService.Swap(player, parsed, chosen, Timer, mode: ExecutionMode.Debug))
      throw new CommandException($"{player.PlayerName} has no live pawn.");

    AdminCommand.Reply(
      caller,
      $"[Loadout] {player.PlayerName}: {catalog.DisplayName(parsed)} - {chosen.Name} (applies in {LoadoutService.SwapDelaySeconds:0.#}s)");
  }

  [Command("loadout_copy", Description = "Copy one player's exact hero, items, ability upgrades and level onto another: loadout_copy <from> <to>")]
  public void CmdCopy(CCitadelPlayerController? caller, int from, int to)
  {
    AdminCommand.Authorize(caller, CommandsLog, "loadout_copy");

    var source = BySlot(from);
    var target = BySlot(to);
    var pawn = source.GetHeroPawn();

    if (pawn == null || !pawn.IsAlive)
      throw new CommandException($"{source.PlayerName} has no live hero to copy.");

    var snapshot = LoadoutService.Capture(pawn, source.PlayerName);

    if (!LoadoutService.SwapSnapshot(target, snapshot, Timer, mode: ExecutionMode.Debug))
      throw new CommandException($"{target.PlayerName} has no live pawn.");

    AdminCommand.Reply(caller, $"[Loadout] {source.PlayerName} -> {target.PlayerName}: {snapshot.Describe()}");
  }

  [Command("loadout_show", Description = "Show a player's held items with soul costs, level and ability ranks: loadout_show <slot>")]
  public void CmdShow(CCitadelPlayerController? caller, int slot)
  {
    AdminCommand.Authorize(caller, CommandsLog, "loadout_show");

    var player = BySlot(slot);
    var pawn = player.GetHeroPawn()
      ?? throw new CommandException($"{player.PlayerName} has no hero.");

    var lines = LoadoutService.Capture(pawn, player.PlayerName).HeldLines(HeroBuildCatalog.Default.CostOf);

    CommandsLog.Info(player.ToPlayerRef(), "Held items {Items}", string.Join(" | ", lines));

    foreach (var line in lines)
      AdminCommand.Reply(caller, $"[Loadout] {line}");
  }

  [Command("loadout_list", Description = "List the stored top builds for a hero: loadout_list <hero>")]
  public void CmdList(CCitadelPlayerController? caller, string hero)
  {
    AdminCommand.Authorize(caller, CommandsLog, "loadout_list");

    var catalog = HeroBuildCatalog.Default;

    if (!catalog.TryParseHero(hero, out var parsed))
      throw new CommandException($"Unknown hero '{hero}'.");

    var builds = catalog.BuildsFor(parsed);
    var cap = LoadoutService.MaxValue;
    AdminCommand.Reply(caller, $"[Loadout] {catalog.DisplayName(parsed)}: {builds.Count} build(s) at Cap={Format(cap)}");

    for (var i = 0; i < builds.Count; i++)
    {
      var entry = builds[i];
      var shop = catalog.Plan(entry, cap);
      var optional = entry.Categories?.Count(category => category.Optional) ?? 0;

      AdminCommand.Reply(
        caller,
        $"[Loadout] {i + 1}. {entry.Name} | BuildId={entry.BuildId} | Rank={entry.Rank} | Matches={entry.Matches} | " +
        $"Wins={entry.Wins} | Value={shop.Value} | Sold={shop.Sold.Count} | Skipped={shop.Skipped.Count} | " +
        $"Filled={shop.Filled.Count} | Upgraded={shop.Upgraded.Count} | OptionalGroups={optional}");
      AdminCommand.Reply(caller, $"[Loadout]    {string.Join(", ", shop.Items)}");
    }
  }

  [Command("loadout_info", Description = "Show the build data date, source, hero count, baseline value, and cap")]
  public void CmdInfo(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, CommandsLog, "loadout_info");

    var catalog = HeroBuildCatalog.Default;
    var data = catalog.Data;

    AdminCommand.Reply(
      caller,
      $"[Loadout] Fetched={data.FetchedAt} | Source={data.Source} | Window={data.WindowDays}d | Heroes={catalog.Heroes.Count}");
    AdminCommand.Reply(
      caller,
      $"[Loadout] Baseline={catalog.BaselineValue} (median planned value at {Format(LoadoutPlanner.DefaultCap)}, {ItemSlots.ForSouls(LoadoutPlanner.DefaultCap)} items) | Cap={LoadoutService.MaxValue} | " +
      $"Banned={string.Join(",", LoadoutPlanner.Banned)}");
    AdminCommand.Reply(
      caller,
      $"[Loadout] Item limit by cap: {ItemSlots.Describe()} (optional fill and upgrades only at {ItemSlots.MaxSlots})");
  }

  [Command("loadout_cap", Description = "Show or set the loadout soul cap: loadout_cap [souls|default]")]
  public void CmdCap(CCitadelPlayerController? caller, string souls = "")
  {
    AdminCommand.Authorize(caller, CommandsLog, "loadout_cap");

    if (string.IsNullOrWhiteSpace(souls))
    {
      AdminCommand.Reply(
        caller,
        $"[Loadout] Cap={Format(LoadoutService.MaxValue)} souls, {ItemSlots.ForSouls(LoadoutService.MaxValue)} items (default {Format(LoadoutService.DefaultMaxValue)})");
      return;
    }

    if (!LoadoutPlanner.TryParseCap(souls, out var cap))
      throw new CommandException(
        $"Cap must be a whole number from {Format(LoadoutPlanner.MinCap)} to {Format(LoadoutPlanner.MaxCap)}, or {LoadoutPlanner.DefaultCapWord}.");

    var previous = LoadoutService.MaxValue;
    LoadoutService.SetMaxValue(cap, ExecutionMode.Debug);
    AdminCommand.Reply(
      caller,
      $"[Loadout] Cap {Format(previous)} -> {Format(cap)}, {ItemSlots.ForSouls(cap)} items (applies to the next builds handed out)");
  }

  private static string Format(int souls) => souls.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

  private static CCitadelPlayerController BySlot(int slot) =>
    Players.GetAll().FirstOrDefault(candidate => candidate.Slot == slot)
      ?? throw new CommandException($"No player in slot {slot}.");
}
