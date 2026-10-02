using System.Globalization;
using Bublock.Modules.Hud;
using Bublock.Modules.Loadout;
using Bublock.Modules.Queue;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Balance;
using RiftRoulette.Betting;
using RiftRoulette.GameLoop;
using RiftRoulette.Lobby;
using RiftRoulette.Round;
using RiftRoulette.Stats;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.RandomMode;

public sealed record RandomAssignment(Heroes Hero, HeroBuild Build, int Team);

public static class RandomModeService
{
  public const double JoinerFallbackSeconds = 2.0;

  public static readonly LoadoutOptions Options = new(Gold: 0);

  private static readonly Logger Log = BublockLog.For("Random");

  private static readonly Dictionary<ulong, int> Teams = [];

  private static readonly Dictionary<ulong, Heroes> LastHero = [];

  private static readonly Dictionary<ulong, RandomAssignment> Assignments = [];

  private static readonly Dictionary<ulong, int> Values = [];

  private static readonly HeroLock Lock = new();

  private static readonly PlayerQueue BenchRotation = new();

  private static ulong? _benched;

  private static ulong? _returning;

  private static int? _benchRound;

  private static bool _buildsAnnounced;

  public const string SitOutTitle = "Sitting out";
  public const string SitOutDescription = "You play next round";

  public static HeroReservations Reservations { get; } = new();

  public static HeroBans Bans { get; } = new();

  public static int PendingCount => Lock.PendingCount;

  public static ulong? Benched => _benched;

  public static int FighterCount => Assignments.Count;

  public static void BeginMatch(ExecutionMode mode = ExecutionMode.Clean)
  {
    Clear();

    var current = Humans().ToDictionary(player => player.PlayerSteamId, player => player.TeamNum);

    foreach (var (steamId, team) in TeamBalance.Even(current, Random.Shared))
      Teams[steamId] = team;

    Log.WithMode(mode).Info(
      "Teams balanced Sapphire={Sapphire} Amber={Amber} Moved={Moved}",
      Teams.Values.Count(team => team == RiftRouletteTeams.Sapphire),
      Teams.Values.Count(team => team == RiftRouletteTeams.Amber),
      Teams.Count(pair => current[pair.Key] != pair.Value));
  }

  public static int PrepareRound(ITimer timer, ExecutionMode mode = ExecutionMode.Clean, bool forceBalance = false)
  {
    var log = Log.WithMode(mode);
    var catalog = HeroBuildCatalog.Default;
    var players = Humans();
    var connected = players.Select(player => player.PlayerSteamId).ToHashSet();

    foreach (var gone in Teams.Keys.Where(steamId => !connected.Contains(steamId)).ToList())
      Teams.Remove(gone);

    foreach (var player in players.Where(player => !Teams.ContainsKey(player.PlayerSteamId)))
    {
      Teams[player.PlayerSteamId] = TeamBalance.SmallerTeam(Teams.Values, Random.Shared);
      log.Info(player.ToPlayerRef(), "Late joiner placed Team={Team}", RiftRouletteTeams.Name(Teams[player.PlayerSteamId]));
    }

    // A reroll in the same intermission keeps the bench; only a new intermission rotates it.
    if (_benchRound != MatchService.State.Round)
    {
      _returning = _benched;
      _benched = BenchRule.Next(BenchRotation, connected);
      _benchRound = MatchService.State.Round;
    }
    else if (_benched is { } kept && !connected.Contains(kept))
    {
      _benched = null;
    }

    var fighters = new Dictionary<ulong, int>(
      BenchRule.FightingTeams(Teams, _benched, _returning == _benched ? null : _returning, Random.Shared));

    LogEvened(log, fighters);
    BalanceService.TryBalance(fighters, mode, forceBalance);

    foreach (var (steamId, team) in fighters)
      Teams[steamId] = team;

    if (_benched is { } benchedId && Find(benchedId) is { } benchedPlayer)
      log.Info(benchedPlayer.ToPlayerRef(), "Sitting out this round Players={Players}", connected.Count);

    var banned = Bans.Take(MatchService.State.Round);
    var turns = Reservations.Take(fighters.Keys, MatchService.State.Round, banned);
    var reserved = turns.ToDictionary(turn => turn.SteamId, turn => turn.Hero);
    var priority = Priority(catalog, log);
    var heroes = HeroDraw.Draw(fighters.Keys.ToList(), Unbanned(catalog.Heroes), LastHero, Random.Shared, reserved, priority);

    Assignments.Clear();
    Values.Clear();
    _buildsAnnounced = false;
    Lock.ClearAllPending();
    RoundHeroes.Clear();

    var swapped = 0;

    foreach (var player in players)
    {
      if (!heroes.TryGetValue(player.PlayerSteamId, out var hero))
        continue;

      if (Start(player, Assign(player.PlayerSteamId, hero, catalog), timer, mode))
        swapped++;
      else
        Lock.MarkPending(player.PlayerSteamId);
    }

    foreach (var turn in turns)
      TellTurn(turn, log);

    foreach (var burned in Reservations.Burned)
      TellBurned(burned, log);

    log.Info(
      "Round prepared Players={Players} Swapped={Swapped} Pending={Pending} Reserved={Reserved} Banned={Banned} Burned={Burned}",
      Assignments.Count,
      swapped,
      Lock.PendingCount,
      turns.Count,
      banned.Count,
      Reservations.Burned.Count);
    StatsService.RefreshBoards(mode);
    return swapped;
  }

  private static void TellTurn(ReservedTurn turn, Logger log)
  {
    if (Find(turn.SteamId) is not { } player)
      return;

    PlayerChat.Send(player, HeroReservations.TurnLine(HeroBuildCatalog.Default.DisplayName(turn.Hero), turn.Use));
    log.Info(player.ToPlayerRef(), "Reserved hero used Hero={Hero} Use={Use} Of={Of}", turn.Hero, turn.Use, HeroReservations.Rounds);
  }

  private static void TellBurned(ReservedTurn turn, Logger log)
  {
    if (Find(turn.SteamId) is not { } player)
      return;

    PlayerChat.Send(player, HeroReservations.BurnedLine(HeroBuildCatalog.Default.DisplayName(turn.Hero), turn.Use));
    log.Info(player.ToPlayerRef(), "Reserved hero banned, round used Hero={Hero} Use={Use} Of={Of}", turn.Hero, turn.Use, HeroReservations.Rounds);
  }

  private static readonly HashSet<Heroes> PriorityWarned = [];

  private static IReadOnlyList<Heroes> Priority(HeroBuildCatalog catalog, Logger log)
  {
    foreach (var hero in PriorityHeroes.List.Where(hero => !catalog.Heroes.Contains(hero)))
    {
      if (PriorityWarned.Add(hero))
        log.Warn("Priority hero has no builds, skipped Hero={Hero}", hero);
    }

    return PriorityHeroes.InPool(catalog.Heroes);
  }

  // When the bans cover the whole pool (a tiny test catalog), they are ignored so the draw still works.
  private static IReadOnlyList<Heroes> Unbanned(IReadOnlyList<Heroes> heroes)
  {
    var allowed = heroes.Where(hero => !Bans.Current.Contains(hero)).ToList();
    return allowed.Count > 0 ? allowed : heroes;
  }

  private static void LogEvened(Logger log, IReadOnlyDictionary<ulong, int> fighters)
  {
    var moved = fighters.Count(pair => Teams.TryGetValue(pair.Key, out var team) && team != pair.Value);

    if (moved == 0)
      return;

    log.Info(
      "Teams evened Moved={Moved} Sapphire={Sapphire} Amber={Amber}",
      moved,
      fighters.Values.Count(team => team == RiftRouletteTeams.Sapphire),
      fighters.Values.Count(team => team == RiftRouletteTeams.Amber));
  }

  // During an intermission the fighters stay even: a joiner fills an odd gap, swaps in with the bench player, or sits out.
  public static void AddJoiner(CCitadelPlayerController player, int team, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var steamId = player.PlayerSteamId;

    Teams[steamId] = team;
    BenchRotation.Join(steamId);

    if (MatchService.State.Phase != MatchPhase.Intermission)
    {
      log.Info(player.ToPlayerRef(), "Joiner placed, hero next intermission Team={Team}", RiftRouletteTeams.Name(team));
      return;
    }

    var fighterTeams = FighterTeams();

    if (fighterTeams.Count % 2 == 1)
    {
      Teams[steamId] = TeamBalance.SmallerTeam(fighterTeams, Random.Shared);
      AssignLate(player, timer, mode);
      return;
    }

    if (_benched is { } benchedId && benchedId != steamId && Find(benchedId) is { } benched)
    {
      _benched = null;
      Teams[benchedId] = RiftRouletteTeams.Other(team);
      AssignLate(player, timer, mode);
      AssignLate(benched, timer, mode);
      ApplyPending(benched, timer, mode);
      log.Info(benched.ToPlayerRef(), "Subbed in with a joiner Joiner={Joiner} Team={Team}", player.PlayerName, RiftRouletteTeams.Name(Teams[benchedId]));
      return;
    }

    _benched = steamId;
    log.Info(player.ToPlayerRef(), "Joiner sitting out this round Team={Team}", RiftRouletteTeams.Name(team));

    if (_buildsAnnounced)
      HudService.Announce(player, SitOutTitle, SitOutDescription, mode);
  }

  // Called before the leaver is removed. In an intermission the bench player takes the leaver's team.
  public static void OnLeave(ulong steamId, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var wasFighter = Assignments.ContainsKey(steamId);
    var hadTeam = Teams.TryGetValue(steamId, out var team);

    Forget(steamId);

    if (!wasFighter || !hadTeam || MatchService.State.Phase != MatchPhase.Intermission)
      return;

    if (_benched is not { } benchedId || Find(benchedId) is not { } benched)
      return;

    _benched = null;
    Teams[benchedId] = team;
    AssignLate(benched, timer, mode);
    ApplyPending(benched, timer, mode);
    log.Info(benched.ToPlayerRef(), "Subbed in for a player who left Team={Team}", RiftRouletteTeams.Name(team));
  }

  private static void AssignLate(CCitadelPlayerController player, ITimer timer, ExecutionMode mode)
  {
    var steamId = player.PlayerSteamId;
    var catalog = HeroBuildCatalog.Default;
    var taken = Assignments.Values.Select(assignment => assignment.Hero).ToHashSet();
    var unbanned = Unbanned(catalog.Heroes);
    var free = unbanned.Where(hero => !taken.Contains(hero)).ToList();
    var pool = free.Count > 0 ? free : unbanned;

    if (pool.Count == 0)
      return;

    // An unheld priority hero (its holder left) goes to the late player; no reserved round is used.
    var unheld = Priority(catalog, Log.WithMode(mode)).Where(hero => !taken.Contains(hero)).ToList();
    var turn = unheld.Count > 0 ? null : Reservations.TakeLate(steamId, MatchService.State.Round, taken, Bans.Current);
    var hero = unheld.Count > 0
      ? unheld[Random.Shared.Next(unheld.Count)]
      : turn?.Hero ?? HeroDraw.Draw([steamId], pool, LastHero, Random.Shared)[steamId];
    Assign(steamId, hero, catalog);

    if (turn != null)
      TellTurn(turn, Log.WithMode(mode));

    Lock.MarkPending(steamId);
    Log.WithMode(mode).Info(player.ToPlayerRef(), "Late hero assigned, pending spawn Hero={Hero} Team={Team}", hero, RiftRouletteTeams.Name(Teams[steamId]));

    timer.Once(JoinerFallbackSeconds.Seconds(), () =>
    {
      if (Find(steamId) is { } current)
        ApplyPending(current, timer, mode);
    });
  }

  private static List<int> FighterTeams()
  {
    var connected = Humans().Select(player => player.PlayerSteamId).ToHashSet();
    return Assignments.Where(pair => connected.Contains(pair.Key)).Select(pair => pair.Value.Team).ToList();
  }

  private static CCitadelPlayerController? Find(ulong steamId) =>
    Players.GetAll().FirstOrDefault(player => player.PlayerSteamId == steamId);

  public static bool ApplyPending(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;

    if (!MatchConfig.IsRandom || !Lock.IsPending(steamId) || !Assignments.TryGetValue(steamId, out var assignment))
      return false;

    if (!Start(player, assignment, timer, mode))
      return false;

    Lock.ClearPending(steamId);
    Log.WithMode(mode).Info(player.ToPlayerRef(), "Pending loadout started after spawn Hero={Hero}", assignment.Hero);
    return true;
  }

  public static bool TryGetAssignment(ulong steamId, out RandomAssignment assignment) =>
    Assignments.TryGetValue(steamId, out assignment!);

  public static bool GuardHero(
    CCitadelPlayerController player,
    CCitadelPlayerPawn pawn,
    ITimer timer,
    ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;

    if (!MatchConfig.IsRandom || !Assignments.TryGetValue(steamId, out var assignment))
      return false;

    Lock.Enforce(
      player,
      pawn,
      assignment.Hero,
      HeroBuildCatalog.Default.DisplayName(assignment.Hero),
      Log.WithMode(mode),
      () => Start(player, assignment, timer, mode));

    return true;
  }

  public static bool ConsumeEnforcementKill(ulong steamId) => Lock.ConsumeKill(steamId);

  public static void Forget(ulong steamId)
  {
    Teams.Remove(steamId);
    Assignments.Remove(steamId);
    Values.Remove(steamId);
    Lock.Forget(steamId);
    BenchRotation.Leave(steamId);

    if (_benched == steamId)
      _benched = null;
  }

  public static int AnnounceBuilds(ExecutionMode mode = ExecutionMode.Clean)
  {
    _buildsAnnounced = true;
    var shown = 0;

    foreach (var player in Players.GetAll())
    {
      if (AnnounceBuild(player, mode))
      {
        shown++;
      }
      else if (_benched == player.PlayerSteamId)
      {
        HudService.Announce(player, SitOutTitle, SitOutDescription, mode);
        shown++;
      }
    }

    return shown;
  }

  public static string BuildDescription(string buildName, int souls) =>
    $"{buildName} - {souls.ToString("N0", CultureInfo.InvariantCulture)} souls";

  private static bool AnnounceBuild(CCitadelPlayerController player, ExecutionMode mode)
  {
    var steamId = player.PlayerSteamId;

    if (!Assignments.TryGetValue(steamId, out var assignment) || !Values.TryGetValue(steamId, out var souls))
      return false;

    HudService.Announce(
      player,
      HeroBuildCatalog.Default.DisplayName(assignment.Hero),
      BuildDescription(assignment.Build.Name, souls),
      mode);

    return true;
  }

  public static int EndMatch(ExecutionMode mode = ExecutionMode.Clean)
  {
    var reset = 0;

    foreach (var player in Players.GetAll())
    {
      var pawn = player.GetHeroPawn();

      if (pawn == null || !pawn.IsAlive || !Assignments.ContainsKey(player.PlayerSteamId))
        continue;

      pawn.ResetHero();
      reset++;
    }

    Log.WithMode(mode).Info("Random match ended HeroesReset={HeroesReset}", reset);
    Clear();
    return reset;
  }

  public static IReadOnlyList<string> Describe()
  {
    var catalog = HeroBuildCatalog.Default;
    var lines = new List<string>
    {
      $"{MatchConfig.Describe()} | Assigned={Assignments.Count} | Pending={Lock.PendingCount} | Teams={Teams.Count} | " +
      $"Bench={(_benched is { } benched ? Find(benched)?.PlayerName ?? benched.ToString() : "none")}",
      $"Bans: this round={HeroList(Bans.Current)} | pending " +
      $"Sapphire={PendingBan(RiftRouletteTeams.Sapphire)} Amber={PendingBan(RiftRouletteTeams.Amber)}",
      $"Priority={DescribePriority(catalog)}"
    };

    foreach (var player in Players.GetAll())
    {
      var steamId = player.PlayerSteamId;
      var team = Teams.TryGetValue(steamId, out var number) ? RiftRouletteTeams.Name(number) : "-";
      var hero = Assignments.TryGetValue(steamId, out var assignment)
        ? $"{catalog.DisplayName(assignment.Hero)} | Build={assignment.Build.Name} ({assignment.Build.BuildId})"
        : "-";
      var pending = Lock.IsPending(steamId) ? " | PENDING" : "";
      var sitting = _benched == steamId ? " | SITTING OUT" : "";
      var reservation = Reservations.Position(steamId) switch
      {
        null => "",
        { Place: 0 } held => $" | Reserved={catalog.DisplayName(held.Hero)} ({held.RoundsLeft} left)",
        var waiting => $" | Waiting={catalog.DisplayName(waiting.Hero)} #{waiting.Place + 1}"
      };

      lines.Add($"Slot={player.Slot} | {player.PlayerName} | Team={team} | Hero={hero}{pending}{sitting}{reservation}");
    }

    return lines;
  }

  private static string DescribePriority(HeroBuildCatalog catalog) =>
    PriorityHeroes.List.Count == 0
      ? "none"
      : string.Join(", ", PriorityHeroes.List.Order().Select(hero =>
          $"{catalog.DisplayName(hero)} ({(catalog.Heroes.Contains(hero) ? "in pool" : "no builds")})"));

  private static string HeroList(IEnumerable<Heroes> heroes)
  {
    var names = heroes.Select(HeroBuildCatalog.Default.DisplayName).Order().ToList();
    return names.Count == 0 ? "none" : string.Join(", ", names);
  }

  private static string PendingBan(int team) =>
    Bans.TryGetPending(team, out var hero, out var by)
      ? $"{HeroBuildCatalog.Default.DisplayName(hero)} ({PlayerName(by)})"
      : "none";

  public static string Reserve(CCitadelPlayerController player, string heroText, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!BettingService.Active)
      return "Hero reservations are only open during a Random mode match.";

    if (!Participants.IsParticipant(player))
      return "Spectators can't reserve heroes.";

    var text = heroText.Trim();

    if (text.Length == 0)
      return DescribeReservation(player);

    var catalog = HeroBuildCatalog.Default;
    var steamId = player.PlayerSteamId;

    if (!TryParseReservable(catalog, text, out var hero))
      return $"No hero called '{text}'. Use the hero's name, for example /reserve haze.";

    if (PriorityHeroes.Contains(hero))
      return PriorityHeroes.RefusedLine(catalog.DisplayName(hero), "reserved");

    if (Reservations.Position(steamId) != null)
      return $"You already have a reservation. {DescribeReservation(player)}";

    var book = BettingService.Book;

    if (!book.TrySpend(steamId, HeroReservations.Cost))
    {
      var riding = book.TryGetBet(steamId, out _) ? " Your souls are on a bet until the round ends." : "";
      return $"A reservation costs {BetBoardText.Format(HeroReservations.Cost)} souls; you have {BetBoardText.Format(book.Chips(steamId))}.{riding}";
    }

    var outcome = Reservations.TryReserve(steamId, hero);
    var name = catalog.DisplayName(hero);
    var left = $"{BetBoardText.Format(book.Chips(steamId))} souls left";

    Log.WithMode(mode).Info(
      player.ToPlayerRef(),
      "Hero reserved Hero={Hero} Result={Result} Ahead={Ahead} RoundsAhead={RoundsAhead} Chips={Chips}",
      hero,
      outcome.Result,
      outcome.Ahead,
      outcome.RoundsAhead,
      book.Chips(steamId));
    BettingService.RefreshBoard(mode);

    if (outcome.Result == ReserveResult.Waiting)
      return $"{HeroReservations.WaitingLine(name, outcome.Ahead, outcome.RoundsAhead)} ({left})";

    var from = MatchService.State.Phase == MatchPhase.Intermission ? "the round after this one" : "the next round you play";
    return $"Reserved {name} for your next {HeroReservations.Rounds} rounds, starting {from} ({left}).";
  }

  public static string DescribeReservation(CCitadelPlayerController player)
  {
    var steamId = player.PlayerSteamId;

    if (Reservations.Position(steamId) is not { } position)
      return $"Reserve a hero for your next {HeroReservations.Rounds} rounds: /reserve <hero> " +
             $"({BetBoardText.Format(HeroReservations.Cost)} souls; you have {BetBoardText.Format(BettingService.Book.Chips(steamId))}).";

    var name = HeroBuildCatalog.Default.DisplayName(position.Hero);

    return position.Place == 0
      ? $"Your reservation: {name}, {HeroReservations.RoundCount(position.RoundsLeft)} left."
      : $"You're #{position.Place + 1} in line for {name}: {HeroReservations.RoundCount(position.RoundsAhead)} ahead of you, " +
        $"then {HeroReservations.RoundCount(position.RoundsLeft)} for you.";
  }

  // One ban per team per round; the hero leaves the next draw for both teams. Bought mid-intermission, it waits a round.
  public static string Ban(CCitadelPlayerController player, string heroText, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!BettingService.Active)
      return "Hero bans are only open during a Random mode match.";

    if (!Participants.IsParticipant(player))
      return "Spectators can't ban heroes.";

    var steamId = player.PlayerSteamId;

    if (!Teams.TryGetValue(steamId, out var team))
      return "You need a team to ban a hero. Try again next round.";

    var text = heroText.Trim();

    if (text.Length == 0)
      return DescribeBan(player, team);

    var catalog = HeroBuildCatalog.Default;

    if (!TryParseReservable(catalog, text, out var hero))
      return $"No hero called '{text}'. Use the hero's name, for example /heroban haze.";

    if (PriorityHeroes.Contains(hero))
      return PriorityHeroes.RefusedLine(catalog.DisplayName(hero), "banned");

    if (Bans.TryGetPending(team, out var pending, out var by))
      return $"Your team already banned {catalog.DisplayName(pending)} ({PlayerName(by)}). One ban per team per round.";

    var book = BettingService.Book;

    if (!book.TrySpend(steamId, HeroBans.Cost))
    {
      var riding = book.TryGetBet(steamId, out _) ? " Your souls are on a bet until the round ends." : "";
      return $"A hero ban costs {BetBoardText.Format(HeroBans.Cost)} souls; you have {BetBoardText.Format(book.Chips(steamId))}.{riding}";
    }

    Bans.TryBan(team, steamId, hero);
    Log.WithMode(mode).Info(
      player.ToPlayerRef(),
      "Hero banned Hero={Hero} Team={Team} Chips={Chips}",
      hero,
      RiftRouletteTeams.Name(team),
      book.Chips(steamId));
    BettingService.RefreshBoard(mode);

    var when = MatchService.State.Phase == MatchPhase.Intermission ? "the round after this one" : "next round";
    return $"Banned {catalog.DisplayName(hero)} for both teams {when} " +
           $"({BetBoardText.Format(book.Chips(steamId))} souls left). Everyone sees it when that round starts.";
  }

  private static string DescribeBan(CCitadelPlayerController player, int team)
  {
    if (Bans.TryGetPending(team, out var hero, out var by))
      return $"Your team's ban: {HeroBuildCatalog.Default.DisplayName(hero)} ({PlayerName(by)}), for the next draw.";

    return $"Ban a hero for both teams for one round: /heroban <hero> " +
           $"({BetBoardText.Format(HeroBans.Cost)} souls; you have {BetBoardText.Format(BettingService.Book.Chips(player.PlayerSteamId))}).";
  }

  // Chat only: the heroes, never who banned them.
  public static int AnnounceBans(ExecutionMode mode = ExecutionMode.Clean)
  {
    if (Bans.Current.Count == 0)
      return 0;

    var catalog = HeroBuildCatalog.Default;
    var line = HeroBans.RevealLine(Bans.Current.Select(catalog.DisplayName).Order());
    var told = 0;

    foreach (var player in Players.GetAll())
    {
      PlayerChat.Send(player, line);
      told++;
    }

    Log.WithMode(mode).Info("Bans revealed Heroes={Heroes} Told={Told}", string.Join(",", Bans.Current), told);
    return told;
  }

  private static bool TryParseReservable(HeroBuildCatalog catalog, string text, out Heroes hero) =>
    (catalog.TryParseHero(text, out hero) || catalog.TryParseHero(text.Replace(" ", ""), out hero))
    && catalog.Heroes.Contains(hero);

  private static string PlayerName(ulong steamId) =>
    Find(steamId) is { } found ? found.PlayerName : "a teammate";

  private static RandomAssignment Assign(ulong steamId, Heroes hero, HeroBuildCatalog catalog)
  {
    var builds = catalog.BuildsFor(hero);
    var assignment = new RandomAssignment(hero, builds[Random.Shared.Next(builds.Count)], Teams[steamId]);

    Assignments[steamId] = assignment;
    LastHero[steamId] = hero;
    Lock.Unapply(steamId);

    RoundHeroes.Set(steamId, hero);

    return assignment;
  }

  private static bool Start(CCitadelPlayerController player, RandomAssignment assignment, ITimer timer, ExecutionMode mode)
  {
    var pawn = player.GetHeroPawn();

    if (pawn == null || !pawn.IsAlive)
    {
      Log.WithMode(mode).Debug(player.ToPlayerRef(), "Swap deferred, player dead Hero={Hero}", assignment.Hero);
      return false;
    }

    if (player.TeamNum != assignment.Team)
      player.ChangeTeam(assignment.Team);

    var steamId = player.PlayerSteamId;
    Lock.Unapply(steamId);

    return LoadoutService.Swap(
      player,
      assignment.Hero,
      assignment.Build,
      timer,
      Options,
      mode,
      (current, result) =>
      {
        if (!Assignments.TryGetValue(steamId, out var live) || live != assignment)
          return;

        Lock.MarkApplied(steamId);
        Values[steamId] = result.Value;

        if (_buildsAnnounced)
          AnnounceBuild(current, mode);
      });
  }

  private static List<CCitadelPlayerController> Humans() => Participants.Humans();

  private static void Clear()
  {
    Teams.Clear();
    LastHero.Clear();
    Assignments.Clear();
    Values.Clear();
    _buildsAnnounced = false;
    Lock.Clear();
    BenchRotation.Clear();
    _benched = null;
    _returning = null;
    _benchRound = null;
    Reservations.Reset();
    Bans.Reset();
  }
}
