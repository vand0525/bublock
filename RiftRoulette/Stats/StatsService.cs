using Bublock.Modules.WorldText;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Balance;
using RiftRoulette.Betting;
using RiftRoulette.Draft;
using RiftRoulette.Duel;
using RiftRoulette.GameLoop;
using RiftRoulette.Lobby;
using RiftRoulette.RandomMode;

namespace RiftRoulette.Stats;

public static class StatsService
{
  public const string SapphireBoard = "stats.sapphire";
  public const string AmberBoard = "stats.amber";

  private static readonly Logger Log = BublockLog.For("Stats");

  private static int _sapphireRounds;
  private static int _amberRounds;

  public static StatsLedger Ledger { get; } = new();

  public static void Reset(ExecutionMode mode = ExecutionMode.Clean)
  {
    Ledger.Reset();
    _sapphireRounds = MatchService.State.Sapphire;
    _amberRounds = MatchService.State.Amber;

    Log.WithMode(mode).Info("Match stats reset");
    RefreshBoards(mode);
  }

  public static void SetRounds(int sapphire, int amber, ExecutionMode mode = ExecutionMode.Clean)
  {
    _sapphireRounds = sapphire;
    _amberRounds = amber;
    RefreshBoards(mode);
  }

  public static void RecordDeath(PlayerDeathEvent args, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!MatchService.State.IsRunning)
      return;

    var log = Log.WithMode(mode);
    var victim = Human(args.UseridController);

    if (victim == null)
      return;

    if (RandomModeService.ConsumeEnforcementKill(victim.PlayerSteamId) || DuelService.ConsumeEnforcementKill(victim.PlayerSteamId))
    {
      log.Debug(victim.ToPlayerRef(), "Death skipped, hero swap enforcement");
      return;
    }

    var attacker = Human(args.AttackerController);
    var assisters = new[]
      {
        args.Assister1controller,
        args.Assister2controller,
        args.Assister3controller,
        args.Assister4controller,
        args.Assister5controller
      }
      .Select(Human)
      .OfType<CCitadelPlayerController>()
      .ToList();

    var credited = Ledger.RecordDeath(
      ToParticipant(victim),
      attacker == null ? null : ToParticipant(attacker),
      assisters.Select(ToParticipant));

    if (credited != null)
    {
      BalanceService.RecordKill(credited.Team);
      BettingService.OnKill(attacker!.PlayerSteamId, mode);
      BettingService.OnAssists(credited.Assisters, mode);
    }

    log.Debug(
      victim.ToPlayerRef(),
      "Death recorded Attacker={Attacker} Assisters={Assisters} CreditedTeam={CreditedTeam} CreditedAssists={CreditedAssists}",
      attacker?.PlayerName ?? "-",
      string.Join(",", assisters.Select(assister => assister.PlayerName)),
      credited != null ? RiftRouletteTeams.Name(credited.Team) : "-",
      credited?.Assisters.Count ?? 0);

    RefreshBoards(mode);
  }

  public static void RefreshBoards(ExecutionMode mode = ExecutionMode.Clean)
  {
    if (MatchConfig.UsesDraft)
      return;

    var live = WorldTextService.List().Select(board => board.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

    if (MatchConfig.IsDuel)
    {
      var text = StatsBoardText.StreakBoard(DuelService.StreakRows());
      Write(SapphireBoard, RiftRouletteTeams.Sapphire, text, live, mode);
      Write(AmberBoard, RiftRouletteTeams.Amber, text, live, mode);
      return;
    }

    Draw(SapphireBoard, RiftRouletteTeams.Sapphire, live, mode);
    Draw(AmberBoard, RiftRouletteTeams.Amber, live, mode);
  }

  public static IReadOnlyList<string> Describe(CCitadelPlayerController player)
  {
    var humans = Humans();

    return
    [
      $"You (K / D / A): {Ledger.Get(player.PlayerSteamId).Line}",
      $"Sapphire: {TeamTotal(humans, RiftRouletteTeams.Sapphire).Line} | Amber: {TeamTotal(humans, RiftRouletteTeams.Amber).Line}"
    ];
  }

  public static IReadOnlyList<string> DescribeAll()
  {
    var lines = new List<string> { $"Match running={MatchService.State.IsRunning} | Tracked={Ledger.Count} | Rounds Sapphire {_sapphireRounds} - {_amberRounds} Amber" };

    foreach (var player in Humans())
    {
      var stats = Ledger.Get(player.PlayerSteamId);
      lines.Add($"Slot={player.Slot} | {player.PlayerName} | Team={RiftRouletteTeams.Name(player.TeamNum)} | K/D/A={stats.Line} | Score={stats.Score}");
    }

    return lines;
  }

  private static void Draw(string id, int team, HashSet<string> live, ExecutionMode mode)
  {
    var rows = Humans()
      .Where(player => player.TeamNum == team)
      .Select(player => new StatsRow(player.PlayerName, Ledger.Get(player.PlayerSteamId)))
      .ToList();

    var rounds = team == RiftRouletteTeams.Sapphire ? _sapphireRounds : _amberRounds;
    var text = StatsBoardText.TeamBoard(RiftRouletteTeams.Name(team).ToUpperInvariant(), rounds, rows);

    Write(id, team, text, live, mode);
  }

  private static void Write(string id, int team, string text, HashSet<string> live, ExecutionMode mode)
  {
    if (live.Contains(id))
      WorldTextService.Update(id, text, mode);
    else
      WorldTextService.Create(id, BoardLayout.Side(team, text), mode);
  }

  private static PlayerStats TeamTotal(IEnumerable<CCitadelPlayerController> humans, int team) =>
    Ledger.Total(humans.Where(player => player.TeamNum == team).Select(player => player.PlayerSteamId));

  private static Participant ToParticipant(CCitadelPlayerController player) => new(player.PlayerSteamId, player.TeamNum);

  private static CCitadelPlayerController? Human(CBasePlayerController? controller)
  {
    var player = controller?.As<CCitadelPlayerController>();
    return player == null || player.IsBot ? null : player;
  }

  private static List<CCitadelPlayerController> Humans() => Participants.Humans();
}
