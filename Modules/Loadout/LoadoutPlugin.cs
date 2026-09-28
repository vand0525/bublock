using Bublock.Shared;
using DeadworksManaged.Api;

namespace Bublock.Modules.Loadout;

public class LoadoutPlugin : DeadworksPluginBase
{
  private static readonly Logger CommandsLog = BublockLog.For("Loadout");

  public override string Name => "Loadout";

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
