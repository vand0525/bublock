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

  [Command("loadout_list", Description = "List the stored top builds for a hero: loadout_list <hero>")]
  public void CmdList(CCitadelPlayerController? caller, string hero)
  {
    AdminCommand.Authorize(caller, CommandsLog, "loadout_list");

    var catalog = HeroBuildCatalog.Default;

    if (!catalog.TryParseHero(hero, out var parsed))
      throw new CommandException($"Unknown hero '{hero}'.");

    var builds = catalog.BuildsFor(parsed);
    AdminCommand.Reply(caller, $"[Loadout] {catalog.DisplayName(parsed)}: {builds.Count} build(s)");

    for (var i = 0; i < builds.Count; i++)
    {
      var entry = builds[i];
      var first = LoadoutPlanner.FirstSlots(LoadoutPlanner.ItemOrder(entry), catalog.ComponentsOf);
      var optional = entry.Categories?.Count(category => category.Optional) ?? 0;

      AdminCommand.Reply(
        caller,
        $"[Loadout] {i + 1}. {entry.Name} | BuildId={entry.BuildId} | Rank={entry.Rank} | Matches={entry.Matches} | " +
        $"Wins={entry.Wins} | Value={catalog.PlannedValue(entry)} | OptionalGroups={optional}");
      AdminCommand.Reply(caller, $"[Loadout]    {string.Join(", ", first)}");
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
      $"[Loadout] Baseline={catalog.BaselineValue} (median first-{LoadoutPlanner.DefaultSlots} value) | Cap={LoadoutService.DefaultMaxValue} | " +
      $"Banned={string.Join(",", LoadoutPlanner.Banned)}");
  }

  private static CCitadelPlayerController BySlot(int slot) =>
    Players.GetAll().FirstOrDefault(candidate => candidate.Slot == slot)
      ?? throw new CommandException($"No player in slot {slot}.");
}
