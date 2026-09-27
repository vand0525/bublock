using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.SelfTest;

namespace RiftRoulette.Draft;

public class DraftPlugin : DeadworksPluginBase
{
  private static readonly Logger DraftLog = BublockLog.For("Draft");

  public override string Name => "Rift Roulette Draft";

  public override void OnLoad(bool isReload)
  {
    if (isReload)
      Timer.NextTick(() => DraftService.RedrawBoards());
  }

  public override void OnStartupServer()
  {
    Timer.NextTick(() => DraftService.RedrawBoards());
  }

  [GameEventHandler("player_hero_changed")]
  public HookResult OnPlayerHeroChanged(PlayerHeroChangedEvent args)
  {
    EventCounters.Hit("player_hero_changed");
    var pawn = args.Userid?.As<CCitadelPlayerPawn>();

    if (pawn?.Controller == null)
      return HookResult.Continue;

    DraftService.EnforceHero(pawn.Controller, pawn, Timer);
    return HookResult.Continue;
  }

  [Command("pick", Description = "Draft a hero: pick <hero>")]
  public void CmdPick(CCitadelPlayerController player, string hero)
  {
    PlayerChat.Send(player, DraftService.Pick(player, hero, Timer));
  }

  [Command("unpick", Description = "Give your drafted hero back")]
  public void CmdUnpick(CCitadelPlayerController player)
  {
    PlayerChat.Send(player, DraftService.Unpick(player, Timer));
  }

  [Command("picks", Description = "List drafted heroes and who picked them")]
  public void CmdPicks(CCitadelPlayerController player)
  {
    PlayerChat.Send(player, DraftService.DescribePicks());
  }

  [Command("heroes", Description = "Show both hero pools and which heroes are taken")]
  public void CmdHeroes(CCitadelPlayerController player)
  {
    foreach (var line in DraftService.DescribeHeroes())
      PlayerChat.Send(player, line);
  }

  [Command("draft_status", Description = "Show both pools and every pick with player and slot")]
  public void CmdDraftStatus(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, DraftLog, "draft_status");

    foreach (var line in DraftService.DescribeDraft())
      AdminCommand.Reply(caller, $"[Draft] {line}");
  }

  [Command("draft_assign", Description = "Pick a hero for a player: draft_assign <slot> <hero>")]
  public void CmdDraftAssign(CCitadelPlayerController? caller, int slot, string hero)
  {
    AdminCommand.Authorize(caller, DraftLog, "draft_assign");

    var player = ResolvePlayer(slot);
    var result = DraftService.Pick(player, hero, Timer, ExecutionMode.Debug);

    PlayerChat.Send(player, result);
    AdminCommand.Reply(caller, $"[Draft] {player.PlayerName}: {result}");
  }

  [Command("draft_release", Description = "Unpick for a player: draft_release <slot>")]
  public void CmdDraftRelease(CCitadelPlayerController? caller, int slot)
  {
    AdminCommand.Authorize(caller, DraftLog, "draft_release");

    var player = ResolvePlayer(slot);
    var result = DraftService.Unpick(player, Timer, ExecutionMode.Debug);

    PlayerChat.Send(player, result);
    AdminCommand.Reply(caller, $"[Draft] {player.PlayerName}: {result}");
  }

  [Command("draft_reset", Description = "Clear the whole draft and return everyone to the lobby")]
  public void CmdDraftReset(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, DraftLog, "draft_reset");

    var reset = DraftService.Reset(Timer, ExecutionMode.Debug);
    AdminCommand.Reply(caller, $"[Draft] Draft reset, {reset} player(s) returned to the lobby");
  }

  [Command("draft_boards", Description = "Redraw the draft boards")]
  public void CmdDraftBoards(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, DraftLog, "draft_boards");

    DraftService.RedrawBoards(ExecutionMode.Debug);
    AdminCommand.Reply(caller, "[Draft] Boards redrawn");
  }

  private static CCitadelPlayerController ResolvePlayer(int slot) =>
    Players.GetAll().FirstOrDefault(player => player.Slot == slot)
      ?? throw new CommandException($"No player in slot {slot}.");
}
