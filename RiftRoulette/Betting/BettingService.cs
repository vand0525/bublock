using Bublock.Modules.WorldText;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Draft;
using RiftRoulette.GameLoop;
using RiftRoulette.Lobby;
using RiftRoulette.RandomMode;

namespace RiftRoulette.Betting;

public static class BettingService
{
  public const string BoardId = "bets.leaders";

  public const int LingerSeconds = 10;

  private static readonly Logger Log = BublockLog.For("Betting");

  private static readonly int[] BothTeams = [RiftRouletteTeams.Sapphire, RiftRouletteTeams.Amber];

  public static BetBook Book { get; } = new();

  public static bool IsOpen { get; private set; }

  public static bool Active => MatchConfig.IsRandom && MatchService.State.IsRunning;

  public static void Reset(ExecutionMode mode = ExecutionMode.Clean)
  {
    Book.Reset();
    IsOpen = false;
    Log.WithMode(mode).Info("Betting reset StartingChips={StartingChips}", BetBook.StartingChips);
    RefreshBoard(mode);
  }

  public static int Open(ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!Active)
      return 0;

    IsOpen = true;
    var told = 0;

    foreach (var player in Participants.Humans())
    {
      var steamId = player.PlayerSteamId;

      if (Book.TryGetBet(steamId, out _) || Book.Chips(steamId) <= 0)
        continue;

      PlayerChat.Send(player, OpenLine(player));
      told++;
    }

    Log.WithMode(mode).Info("Betting open Told={Told}", told);
    return told;
  }

  public static void Close(ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!IsOpen)
      return;

    IsOpen = false;
    Log.WithMode(mode).Info("Betting closed Bets={Bets} Staked={Staked}", Book.OpenBets, Book.Bets.Values.Sum(bet => bet.Stake));
  }

  // Returns the reply for the player; null when the text is not a team name.
  public static string? TryBet(CCitadelPlayerController player, string text, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!RiftRouletteTeams.TryParse(text.Trim(), out var team))
      return null;

    if (!Active)
      return "Betting is only open during a Random mode match.";

    if (!Participants.IsParticipant(player))
      return "Spectators can't bet.";

    if (!IsOpen)
      return "Betting is closed - it opens again after this round.";

    var steamId = player.PlayerSteamId;
    var allowed = AllowedTeams(player);
    var result = Book.Place(steamId, team, allowed);

    Log.WithMode(mode).Info(player.ToPlayerRef(), "Bet Result={Result} Team={Team} Chips={Chips}", result, RiftRouletteTeams.Name(team), Book.Total(steamId));

    if (result is BetResult.Placed or BetResult.Changed)
      RefreshBoard(mode);

    Book.TryGetBet(steamId, out var bet);

    return result switch
    {
      BetResult.Placed => $"Bet {BetBoardText.Format(bet.Stake)} chips on {RiftRouletteTeams.Name(team)}.",
      BetResult.Changed => $"Bet moved: {BetBoardText.Format(bet.Stake)} chips on {RiftRouletteTeams.Name(team)}.",
      BetResult.NoChips => "No chips - get a kill.",
      _ => $"You're fighting for {RiftRouletteTeams.Name(allowed[0])} - you can only bet on your team."
    };
  }

  public static void OnKill(ulong killerId, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!Active)
      return;

    var chips = Book.AwardKill(killerId);
    Log.WithMode(mode).Debug("Kill chips awarded SteamId={SteamId} Chips={Chips}", killerId, chips);
    RefreshBoard(mode);
  }

  public static int OnRoundEnded(int? winner, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!MatchConfig.IsRandom)
      return 0;

    IsOpen = false;
    return Pay(Book.Settle(winner), winner == null ? "no winner" : RiftRouletteTeams.Name(winner.Value), mode);
  }

  public static int EndMatch(ExecutionMode mode = ExecutionMode.Clean)
  {
    IsOpen = false;
    return Pay(Book.RefundAll(), "match ended", mode);
  }

  public static void RefreshBoard(ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!MatchConfig.IsRandom)
      return;

    var rows = Participants.Humans().Select(player => new BetRow(player.PlayerName, Book.Total(player.PlayerSteamId)));
    var text = BetBoardText.Board(rows);

    if (WorldTextService.List().Any(board => string.Equals(board.Id, BoardId, StringComparison.OrdinalIgnoreCase)))
      WorldTextService.Update(BoardId, text, mode);
    else
      WorldTextService.Create(BoardId, BoardLayout.Back(text), mode);
  }

  public static IReadOnlyList<string> DescribePlayer(CCitadelPlayerController player)
  {
    var steamId = player.PlayerSteamId;
    var lines = new List<string> { $"You have {BetBoardText.Format(Book.Chips(steamId))} chips." };

    if (Book.TryGetBet(steamId, out var bet))
      lines.Add($"Your bet: {BetBoardText.Format(bet.Stake)} chips on {RiftRouletteTeams.Name(bet.Team)}.");

    lines.Add(IsOpen ? "Betting is open: type sapphire or amber." : "Betting opens between rounds.");
    return lines;
  }

  public static IReadOnlyList<string> Describe()
  {
    var lines = new List<string> { $"Active={Active} | Open={IsOpen} | Bets={Book.OpenBets} | Staked={Book.Bets.Values.Sum(bet => bet.Stake)}" };

    foreach (var player in Participants.Humans())
    {
      var steamId = player.PlayerSteamId;
      var bet = Book.TryGetBet(steamId, out var open) ? $"{open.Stake} on {RiftRouletteTeams.Name(open.Team)}" : "-";
      lines.Add($"Slot={player.Slot} | {player.PlayerName} | Chips={Book.Chips(steamId)} | Bet={bet}");
    }

    return lines;
  }

  // Players in this round's fight may only back their own team.
  private static int[] AllowedTeams(CCitadelPlayerController player) =>
    RandomModeService.TryGetAssignment(player.PlayerSteamId, out var assignment) ? [assignment.Team] : BothTeams;

  private static string OpenLine(CCitadelPlayerController player)
  {
    var chips = BetBoardText.Format(Book.Chips(player.PlayerSteamId));
    var teams = AllowedTeams(player);
    var how = teams.Length == 1
      ? $"type {RiftRouletteTeams.Name(teams[0]).ToLowerInvariant()} to bet on your team"
      : "type sapphire or amber";

    return $"Bet on the next round: {how} (open until {LingerSeconds}s into the round). You have {chips} chips; a win doubles them.";
  }

  private static int Pay(IReadOnlyList<BetSettlement> settled, string reason, ExecutionMode mode)
  {
    var log = Log.WithMode(mode);

    foreach (var settlement in settled)
    {
      var stake = settlement.Bet.Stake;
      var player = Players.GetAll().FirstOrDefault(candidate => candidate.PlayerSteamId == settlement.SteamId);
      var line = settlement.Outcome switch
      {
        BetOutcome.Won => $"You won {BetBoardText.Format(stake * BetBook.PayoutMultiplier)} chips (now {BetBoardText.Format(settlement.Balance)}).",
        BetOutcome.Lost => $"You lost {BetBoardText.Format(stake)} chips (now {BetBoardText.Format(settlement.Balance)}).",
        _ => $"No result - your {BetBoardText.Format(stake)} chips are back."
      };

      if (player != null)
      {
        PlayerChat.Send(player, line);
        log.Info(player.ToPlayerRef(), "Bet settled Outcome={Outcome} Stake={Stake} Balance={Balance}", settlement.Outcome, stake, settlement.Balance);
      }
      else
      {
        log.Info("Bet settled, player gone SteamId={SteamId} Outcome={Outcome} Stake={Stake}", settlement.SteamId, settlement.Outcome, stake);
      }
    }

    if (settled.Count > 0)
      log.Info("Bets settled Reason={Reason} Bets={Bets}", reason, settled.Count);

    RefreshBoard(mode);
    return settled.Count;
  }
}
