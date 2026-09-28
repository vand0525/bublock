using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.GameLoop;
using RiftRoulette.SelfTest;

namespace RiftRoulette.RandomMode;

public class RandomPlugin : DeadworksPluginBase
{
  private static readonly Logger RandomLog = BublockLog.For("Random");

  public override string Name => "Rift Roulette Random";

  [GameEventHandler("player_respawned")]
  public HookResult OnPlayerRespawned(PlayerRespawnedEvent args)
  {
    EventCounters.Hit("player_respawned");
    ApplyPendingNextTick(args.Userid?.As<CCitadelPlayerPawn>()?.Controller);
    return HookResult.Continue;
  }

  [GameEventHandler("player_spawn")]
  public HookResult OnPlayerSpawn(PlayerSpawnEvent args)
  {
    ApplyPendingNextTick(args.UseridController?.As<CCitadelPlayerController>());
    return HookResult.Continue;
  }

  private void ApplyPendingNextTick(CCitadelPlayerController? player)
  {
    if (player == null || player.IsBot || RandomModeService.PendingCount == 0)
      return;

    var steamId = player.PlayerSteamId;

    Timer.NextTick(() =>
    {
      var current = Players.GetAll().FirstOrDefault(candidate => candidate.PlayerSteamId == steamId);

      if (current != null)
        RandomModeService.ApplyPending(current, Timer);
    });
  }

  [Command("reserve", Description = "Spend 1,000 chips to play a hero in your next 3 rounds: reserve <hero>")]
  public void CmdReserve(CCitadelPlayerController player, params string[] hero)
  {
    PlayerChat.Send(player, RandomModeService.Reserve(player, string.Join(' ', hero)));
  }

  [Command("random_status", Description = "Show Random mode teams, heroes, builds, and pending swaps")]
  public void CmdRandomStatus(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, RandomLog, "random_status");

    foreach (var line in RandomModeService.Describe())
      AdminCommand.Reply(caller, $"[Random] {line}");
  }

  [Command("random_reroll", Description = "Give everyone a new random hero and build now (intermission only)")]
  public void CmdRandomReroll(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, RandomLog, "random_reroll");

    if (!MatchConfig.IsRandom || MatchService.State.Phase != MatchPhase.Intermission)
      throw new CommandException("Reroll works only during a Random mode intermission.");

    var swapped = RandomModeService.PrepareRound(Timer, ExecutionMode.Debug);
    AdminCommand.Reply(caller, $"[Random] Rerolled: {swapped} swapped, {RandomModeService.PendingCount} pending");
  }
}
