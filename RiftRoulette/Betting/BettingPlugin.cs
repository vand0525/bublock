using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Lobby;

namespace RiftRoulette.Betting;

public class BettingPlugin : DeadworksPluginBase
{
  private static readonly Logger BettingLog = BublockLog.For("Betting");

  public override string Name => "Rift Roulette Betting";

  // A chat line that is exactly a team name is a bet; the line still shows in chat.
  public override HookResult OnChatMessage(ChatMessage message)
  {
    var text = message.ChatText.Trim();

    if (!BettingService.Active || !RiftRouletteTeams.TryParse(text, out _))
      return HookResult.Continue;

    var player = message.Controller;

    if (player == null || player.IsBot)
      return HookResult.Continue;

    var steamId = player.PlayerSteamId;

    Timer.NextTick(() =>
    {
      var current = Players.GetAll().FirstOrDefault(candidate => candidate.PlayerSteamId == steamId);

      if (current != null && BettingService.TryBet(current, text) is { } reply)
        PlayerChat.Send(current, reply);
    });

    return HookResult.Continue;
  }

  [Command("bet", Description = "Bet all your chips on a team for the next round: bet <sapphire|amber>")]
  public void CmdBet(CCitadelPlayerController player, string team)
  {
    var reply = BettingService.TryBet(player, team) ?? throw new CommandException("Bet on sapphire or amber.");
    PlayerChat.Send(player, reply);
  }

  [Command("chips", Description = "Show your betting chips and your bet")]
  public void CmdChips(CCitadelPlayerController player)
  {
    foreach (var line in BettingService.DescribePlayer(player))
      PlayerChat.Send(player, line);
  }

  [Command("bet_status", Description = "Show every player's betting chips and open bets")]
  public void CmdBetStatus(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, BettingLog, "bet_status");

    BettingService.RefreshBoard(ExecutionMode.Debug);

    foreach (var line in BettingService.Describe())
      AdminCommand.Reply(caller, $"[Betting] {line}");
  }
}
