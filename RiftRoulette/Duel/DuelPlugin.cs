using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.GameLoop;
using RiftRoulette.Lobby;

namespace RiftRoulette.Duel;

public class DuelPlugin : DeadworksPluginBase
{
  private static readonly Logger DuelLog = BublockLog.For("Duel");

  public override string Name => "Rift Roulette Duel";

  [GameEventHandler("player_respawned")]
  public HookResult OnPlayerRespawned(PlayerRespawnedEvent args)
  {
    OnSpawnNextTick(args.Userid?.As<CCitadelPlayerPawn>()?.Controller);
    return HookResult.Continue;
  }

  [GameEventHandler("player_spawn")]
  public HookResult OnPlayerSpawn(PlayerSpawnEvent args)
  {
    OnSpawnNextTick(args.UseridController?.As<CCitadelPlayerController>());
    return HookResult.Continue;
  }

  private void OnSpawnNextTick(CCitadelPlayerController? player)
  {
    if (player == null || !MatchConfig.IsDuel || !Participants.IsParticipant(player))
      return;

    var steamId = player.PlayerSteamId;

    Timer.NextTick(() =>
    {
      var current = Players.GetAll().FirstOrDefault(candidate => candidate.PlayerSteamId == steamId);

      if (current == null)
        return;

      if (MatchService.State.IsRunning)
        DuelService.ApplyPending(current, Timer);
      else
        DuelService.GrantSetup(current, Timer);
    });
  }

  [Command("duel_copy", Description = "1v1: copy this player's exact hero, items, abilities and level onto both players and start: duel_copy <slot>")]
  public void CmdDuelCopy(CCitadelPlayerController? caller, int slot)
  {
    AdminCommand.Authorize(caller, DuelLog, "duel_copy");

    var source = Players.GetAll().FirstOrDefault(candidate => candidate.Slot == slot)
      ?? throw new CommandException($"No player in slot {slot}.");

    AdminCommand.Reply(caller, $"[Duel] {DuelService.Copy(source, Timer, ExecutionMode.Debug)}");
  }

  [Command("duel_clear", Description = "1v1: drop the copied build and go back to free hero switching (no match running)")]
  public void CmdDuelClear(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, DuelLog, "duel_clear");
    AdminCommand.Reply(caller, $"[Duel] {DuelService.ClearSnapshot(Timer, ExecutionMode.Debug)}");
  }

  [Command("duel_status", Description = "1v1: show the copied build, lock state and competitors")]
  public void CmdDuelStatus(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, DuelLog, "duel_status");

    foreach (var line in DuelService.Describe())
      AdminCommand.Reply(caller, $"[Duel] {line}");
  }

  [Command("queue", Description = "1v1: join the queue (winner stays on), or show your place in it")]
  public void CmdQueue(CCitadelPlayerController player)
  {
    PlayerChat.Send(player, DuelService.JoinQueue(player));
    AutoStartService.Check(Timer);
  }

  [Command("unqueue", Description = "1v1: leave the queue")]
  public void CmdUnqueue(CCitadelPlayerController player)
  {
    PlayerChat.Send(player, DuelService.LeaveQueue(player));
    AutoStartService.Check(Timer);
  }

  [Command("duel_queue", Description = "1v1: list the queue, fighters and the king's streak")]
  public void CmdDuelQueue(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, DuelLog, "duel_queue");

    foreach (var line in DuelService.DescribeQueue())
      AdminCommand.Reply(caller, $"[Duel] {line}");
  }

  [Command("duel_queue_add", Description = "1v1: put a player at the back of the queue: duel_queue_add <slot>")]
  public void CmdDuelQueueAdd(CCitadelPlayerController? caller, int slot)
  {
    AdminCommand.Authorize(caller, DuelLog, "duel_queue_add");

    AdminCommand.Reply(caller, $"[Duel] {DuelService.JoinQueue(BySlot(slot), ExecutionMode.Debug)}");
    AutoStartService.Check(Timer, ExecutionMode.Debug);
  }

  [Command("duel_queue_remove", Description = "1v1: take a player out of the queue (not mid-fight): duel_queue_remove <slot>")]
  public void CmdDuelQueueRemove(CCitadelPlayerController? caller, int slot)
  {
    AdminCommand.Authorize(caller, DuelLog, "duel_queue_remove");

    AdminCommand.Reply(caller, $"[Duel] {DuelService.LeaveQueue(BySlot(slot), ExecutionMode.Debug, force: true)}");
    AutoStartService.Check(Timer, ExecutionMode.Debug);
  }

  private static CCitadelPlayerController BySlot(int slot) =>
    Players.GetAll().FirstOrDefault(candidate => candidate.Slot == slot)
      ?? throw new CommandException($"No player in slot {slot}.");
}
