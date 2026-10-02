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

  public static MarkBook Marks { get; } = new();

  public static bool IsOpen { get; private set; }

  public static bool Active => MatchConfig.IsRandom && MatchService.State.IsRunning;

  // A mark bought in an intermission is for the round about to start; one bought in a round's first seconds, for that round.
  private static int MarkRound =>
    MatchService.State.Phase == MatchPhase.Intermission ? MatchService.State.Round + 1 : MatchService.State.Round;

  public static void Reset(ExecutionMode mode = ExecutionMode.Clean)
  {
    Book.Reset();
    Marks.Reset();
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
      BetResult.Placed => $"Bet {BetBoardText.Format(bet.Stake)} souls on {RiftRouletteTeams.Name(team)}.",
      BetResult.Changed => $"Bet moved: {BetBoardText.Format(bet.Stake)} souls on {RiftRouletteTeams.Name(team)}.",
      BetResult.NoChips => "No souls - get a kill or an assist.",
      _ => $"You're fighting for {RiftRouletteTeams.Name(allowed[0])} - you can only bet on your team."
    };
  }

  public static IReadOnlyList<string> TryMark(CCitadelPlayerController player, string text, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!Active)
      return ["Marks are only open during a Random mode match."];

    if (!Participants.IsParticipant(player))
      return ["Spectators can't mark."];

    var steamId = player.PlayerSteamId;

    if (!RandomModeService.TryGetAssignment(steamId, out var assignment))
      return ["Only this round's fighters can mark - you're sitting out."];

    if (!IsOpen)
      return ["Marking is closed - it opens again after this round."];

    var targets = MarkTargets(assignment.Team);
    var input = text.Trim();

    if (input.Length == 0)
      return MarkMenu(player, targets);

    if (Marks.TryGet(steamId, out var held))
      return [$"You already marked {held.TargetName} {RoundWord(held.Round)}. One mark per round."];

    if (!int.TryParse(input, out var slot) || targets.FirstOrDefault(candidate => candidate.Slot == slot) is not { } target)
      return [$"No enemy fighter in slot {input} - type /mark to see them."];

    if (!Book.TrySpend(steamId, MarkBook.Cost))
    {
      var riding = Book.TryGetBet(steamId, out _) ? " Your souls are on a bet until the round ends." : "";
      return [$"A mark costs {BetBoardText.Format(MarkBook.Cost)} souls; you have {BetBoardText.Format(Book.Chips(steamId))}.{riding}"];
    }

    var round = MarkRound;
    var when = RoundWord(round);
    Marks.Place(steamId, target.PlayerSteamId, target.PlayerName, round);
    PlayerChat.Send(target, $"Someone marked you: if they kill you {when}, they take a quarter of your souls.");

    Log.WithMode(mode).Info(
      player.ToPlayerRef(),
      "Mark placed Target={Target} TargetSteamId={TargetSteamId} Round={Round} Chips={Chips}",
      target.PlayerName,
      target.PlayerSteamId,
      round,
      Book.Chips(steamId));
    RefreshBoard(mode);

    return [$"Marked {target.PlayerName} for {when} ({BetBoardText.Format(Book.Chips(steamId))} souls left). Kill them {when} to steal a quarter of their souls."];
  }

  public static void OnKill(ulong killerId, ulong victimId, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!Active)
      return;

    var chips = Book.AwardKill(killerId);
    Log.WithMode(mode).Debug("Kill chips awarded SteamId={SteamId} Chips={Chips}", killerId, chips);

    if (MatchService.State.Phase == MatchPhase.InRound && Marks.TryConsume(killerId, victimId, MatchService.State.Round))
      PayMark(killerId, victimId, mode);

    RefreshBoard(mode);
  }

  public static void OnAssists(IReadOnlyCollection<ulong> assisterIds, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!Active || assisterIds.Count == 0)
      return;

    var log = Log.WithMode(mode);

    foreach (var steamId in assisterIds)
    {
      var chips = Book.AwardAssist(steamId);
      log.Debug("Assist chips awarded SteamId={SteamId} Chips={Chips}", steamId, chips);
    }

    RefreshBoard(mode);
  }

  public static int OnRoundEnded(int? winner, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!MatchConfig.IsRandom)
      return 0;

    IsOpen = false;
    var settled = Pay(Book.Settle(winner), winner == null ? "no winner" : RiftRouletteTeams.Name(winner.Value), mode);
    ExpireMarks(matchEnded: false, winner, mode);
    return settled;
  }

  public static int EndMatch(ExecutionMode mode = ExecutionMode.Clean)
  {
    IsOpen = false;
    var settled = Pay(Book.RefundAll(), "match ended", mode);
    ExpireMarks(matchEnded: true, winner: null, mode);
    return settled;
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
    var lines = new List<string> { $"You have {BetBoardText.Format(Book.Chips(steamId))} souls." };

    if (Book.TryGetBet(steamId, out var bet))
      lines.Add($"Your bet: {BetBoardText.Format(bet.Stake)} souls on {RiftRouletteTeams.Name(bet.Team)}.");

    if (Marks.TryGet(steamId, out var mark))
      lines.Add($"Your mark: {mark.TargetName} ({RoundWord(mark.Round)}).");

    lines.Add(IsOpen ? "Betting is open: type sapphire or amber." : "Betting opens between rounds.");
    lines.Add(RandomModeService.DescribeReservation(player));
    return lines;
  }

  public static IReadOnlyList<string> Describe()
  {
    var lines = new List<string>
    {
      $"Active={Active} | Open={IsOpen} | Bets={Book.OpenBets} | Staked={Book.Bets.Values.Sum(bet => bet.Stake)} | Marks={Marks.Count}"
    };

    foreach (var player in Participants.Humans())
    {
      var steamId = player.PlayerSteamId;
      var bet = Book.TryGetBet(steamId, out var open) ? $"{open.Stake} on {RiftRouletteTeams.Name(open.Team)}" : "-";
      var mark = Marks.TryGet(steamId, out var held) ? $"{held.TargetName} (round {held.Round})" : "-";
      lines.Add($"Slot={player.Slot} | {player.PlayerName} | Souls={Book.Chips(steamId)} | Bet={bet} | Mark={mark}");
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

    var reserve = Book.Chips(player.PlayerSteamId) >= HeroReservations.Cost && RandomModeService.Reservations.Position(player.PlayerSteamId) == null
      ? $" Or /reserve <hero> for {BetBoardText.Format(HeroReservations.Cost)}."
      : "";

    var mark = teams.Length == 1 && Book.Chips(player.PlayerSteamId) >= MarkBook.Cost && !Marks.TryGet(player.PlayerSteamId, out _)
      ? $" Or /mark an enemy for {BetBoardText.Format(MarkBook.Cost)}."
      : "";

    return $"Bet on the next round: {how} (open until {LingerSeconds}s into the round). You have {chips} souls; a win doubles them.{reserve}{mark}";
  }

  private static string RoundWord(int round) =>
    MatchService.State.Phase == MatchPhase.InRound && round == MatchService.State.Round ? "this round" : "next round";

  // Enemy fighters of the marker's team, in slot order.
  private static List<CCitadelPlayerController> MarkTargets(int markerTeam) =>
    Participants.Humans()
      .Where(candidate => RandomModeService.TryGetAssignment(candidate.PlayerSteamId, out var assignment) && assignment.Team != markerTeam)
      .OrderBy(candidate => candidate.Slot)
      .ToList();

  private static IReadOnlyList<string> MarkMenu(CCitadelPlayerController player, IReadOnlyList<CCitadelPlayerController> targets)
  {
    var steamId = player.PlayerSteamId;
    var when = RoundWord(MarkRound);
    var lines = new List<string>
    {
      $"Mark an enemy fighter for {BetBoardText.Format(MarkBook.Cost)} souls: /mark <slot>. Kill them {when} to steal a quarter of their souls. " +
      $"You have {BetBoardText.Format(Book.Chips(steamId))}."
    };

    if (Marks.TryGet(steamId, out var held))
      lines.Add($"Your mark: {held.TargetName} ({RoundWord(held.Round)}).");

    if (targets.Count == 0)
      lines.Add("No enemy fighters right now.");

    lines.AddRange(targets.Select(target => $"{target.Slot} {target.PlayerName} ({BetBoardText.Format(Book.Total(target.PlayerSteamId))} souls)"));
    return lines;
  }

  private static void PayMark(ulong killerId, ulong victimId, ExecutionMode mode)
  {
    var stolen = Book.Steal(victimId, killerId, MarkBook.StealDivisor);
    var killer = Find(killerId);
    var victim = Find(victimId);
    var victimName = victim?.PlayerName ?? victimId.ToString();

    if (killer != null)
    {
      PlayerChat.Send(killer, stolen > 0
        ? $"You stole {BetBoardText.Format(stolen)} souls from {victimName} (mark)."
        : $"{victimName} had no souls to steal - your mark is used.");
    }

    if (victim != null && stolen > 0)
      PlayerChat.Send(victim, $"{killer?.PlayerName ?? "Your marker"} stole {BetBoardText.Format(stolen)} souls from you - you were marked.");

    Log.WithMode(mode).Info(
      killer?.ToPlayerRef() ?? new PlayerRef(-1, killerId, "-"),
      "Mark paid Victim={Victim} VictimSteamId={VictimSteamId} Stolen={Stolen} Chips={Chips} VictimTotal={VictimTotal}",
      victimName,
      victimId,
      stolen,
      Book.Chips(killerId),
      Book.Total(victimId));
  }

  // Marks last one round. Refunded when the round had no result, the target didn't fight, or the match ended.
  private static void ExpireMarks(bool matchEnded, int? winner, ExecutionMode mode)
  {
    var marks = Marks.TakeAll();

    if (marks.Count == 0)
      return;

    var log = Log.WithMode(mode);
    var cost = BetBoardText.Format(MarkBook.Cost);

    foreach (var (markerId, mark) in marks)
    {
      var targetFought = RandomModeService.TryGetAssignment(mark.TargetId, out _);
      var refund = matchEnded || winner == null || !targetFought;
      var line = matchEnded ? $"Match ended - your {cost} souls for the mark are back."
        : winner == null ? $"No result - your {cost} souls for the mark on {mark.TargetName} are back."
        : !targetFought ? $"{mark.TargetName} didn't fight - your {cost} souls for the mark are back."
        : $"Your mark on {mark.TargetName} ran out.";

      if (refund)
        Book.Refund(markerId, MarkBook.Cost);

      var marker = Find(markerId);

      if (marker != null)
        PlayerChat.Send(marker, line);

      log.Info(
        "Mark ended SteamId={SteamId} Marker={Marker} Target={Target} Round={Round} Refunded={Refunded}",
        markerId,
        marker?.PlayerName ?? "-",
        mark.TargetName,
        mark.Round,
        refund);
    }

    RefreshBoard(mode);
  }

  private static CCitadelPlayerController? Find(ulong steamId) =>
    Players.GetAll().FirstOrDefault(candidate => candidate.PlayerSteamId == steamId);

  private static int Pay(IReadOnlyList<BetSettlement> settled, string reason, ExecutionMode mode)
  {
    var log = Log.WithMode(mode);

    foreach (var settlement in settled)
    {
      var stake = settlement.Bet.Stake;
      var player = Players.GetAll().FirstOrDefault(candidate => candidate.PlayerSteamId == settlement.SteamId);
      var line = settlement.Outcome switch
      {
        BetOutcome.Won => $"You won {BetBoardText.Format(stake * BetBook.PayoutMultiplier)} souls (now {BetBoardText.Format(settlement.Balance)}).",
        BetOutcome.Lost => $"You lost {BetBoardText.Format(stake)} souls (now {BetBoardText.Format(settlement.Balance)}).",
        _ => $"No result - your {BetBoardText.Format(stake)} souls are back."
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
