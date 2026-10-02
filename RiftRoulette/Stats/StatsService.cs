using Bublock.Modules.WorldText;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Balance;
using RiftRoulette.Betting;
using RiftRoulette.Boards;
using RiftRoulette.GameLoop;
using RiftRoulette.Lobby;
using RiftRoulette.Mirror;
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

  public static DamageLedger Damage { get; } = new();

  public static void Reset(ExecutionMode mode = ExecutionMode.Clean)
  {
    Ledger.Reset();
    Damage.Reset();
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

    if (RandomModeService.ConsumeEnforcementKill(victim.PlayerSteamId) ||
        MirrorModeService.ConsumeEnforcementKill(victim.PlayerSteamId))
    {
      log.Debug(victim.ToPlayerRef(), "Death skipped, hero swap enforcement");
      return;
    }

    var attacker = Human(args.AttackerController);
    var maxHealth = args.UseridPawn?.MaxHealth ?? 0;
    var damage = Damage.DamageTo(victim.PlayerSteamId)
      .ToDictionary(entry => entry.Key, entry => (int)Math.Round(entry.Value));
    var connected = Participants.Humans().ToDictionary(player => player.PlayerSteamId);
    var assisters = Damage.Assisters(victim.PlayerSteamId, attacker?.PlayerSteamId ?? 0, maxHealth)
      .Select(steamId => connected.GetValueOrDefault(steamId))
      .OfType<CCitadelPlayerController>()
      .ToList();

    Damage.Clear(victim.PlayerSteamId);

    var credited = Ledger.RecordDeath(
      ToParticipant(victim),
      attacker == null ? null : ToParticipant(attacker),
      assisters.Select(ToParticipant));

    if (credited != null)
    {
      BalanceService.RecordKill(credited.Team);
      BettingService.OnKill(attacker!.PlayerSteamId, victim.PlayerSteamId, mode);
      BettingService.OnAssists(credited.Assisters, mode);
    }

    log.Debug(
      victim.ToPlayerRef(),
      "Death recorded Attacker={Attacker} Assisters={Assisters} CreditedTeam={CreditedTeam} CreditedAssists={CreditedAssists} MaxHealth={MaxHealth} Damage={Damage}",
      attacker?.PlayerName ?? "-",
      string.Join(",", assisters.Select(assister => assister.PlayerName)),
      credited != null ? RiftRouletteTeams.Name(credited.Team) : "-",
      credited?.Assisters.Count ?? 0,
      maxHealth,
      string.Join(",", damage.Select(entry => $"{NameOf(connected, entry.Key)}:{entry.Value}")));

    RefreshBoards(mode);
  }

  // Before the hit lands: the amount is capped at the victim's health left, so overkill never counts.
  public static void RecordDamage(TakeDamageEvent args)
  {
    if (!MatchService.State.IsRunning)
      return;

    var victimPawn = args.Entity.As<CCitadelPlayerPawn>();
    var victim = victimPawn?.Controller;
    var attacker = HeroController(args.Info.Attacker) ?? HeroController(args.Info.Originator);

    if (victimPawn == null || victim == null || attacker == null)
      return;

    if (!Participants.IsParticipant(victim) || !Participants.IsParticipant(attacker))
      return;

    var amount = Math.Min(args.Info.Damage, Math.Max(victimPawn.Health, 0));
    Damage.Record(victim.PlayerSteamId, attacker.PlayerSteamId, amount);
  }

  public static void OnSpawn(CCitadelPlayerController player) => Damage.Clear(player.PlayerSteamId);

  public static void Forget(ulong steamId) => Damage.Forget(steamId);

  private static CCitadelPlayerController? HeroController(CBaseEntity? entity) =>
    entity?.As<CCitadelPlayerPawn>()?.Controller;

  private static string NameOf(Dictionary<ulong, CCitadelPlayerController> connected, ulong steamId) =>
    connected.TryGetValue(steamId, out var player) ? player.PlayerName : steamId.ToString();

  public static void RefreshBoards(ExecutionMode mode = ExecutionMode.Clean)
  {
    var live = WorldTextService.List().Select(board => board.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

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
