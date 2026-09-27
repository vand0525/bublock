using Bublock.Modules.Hud;
using Bublock.Modules.Loadout;
using Bublock.Modules.Queue;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.GameLoop;
using RiftRoulette.Lobby;
using RiftRoulette.Rift;
using RiftRoulette.Stats;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.Duel;

public static class DuelService
{
  public const int SetupGold = 100_000;

  public const int SetupLevel = 36;

  public const int CompetitorCount = 2;

  public const double SetupDelaySeconds = 1.0;

  public const string SetupTitle = "1v1 setup";

  public const string SetupDescription = "Shop open anywhere - build your hero";

  private static readonly Logger Log = BublockLog.For("Duel");

  private static readonly HeroLock Lock = new();

  private static readonly Dictionary<ulong, int> Teams = [];

  private static readonly PlayerQueue Queue = new();

  private static readonly StreakBoard Streaks = new();

  public static LoadoutSnapshot? Snapshot { get; private set; }

  public static bool Locked { get; private set; }

  public static ulong? King { get; private set; }

  public static int Streak { get; private set; }

  public static bool HasSnapshot => Snapshot != null;

  public static int PendingCount => Lock.PendingCount;

  public static bool IsFighter(ulong steamId) => Locked && Teams.ContainsKey(steamId);

  public static bool ReadyToFight => Teams.Count == CompetitorCount;

  public static int QueuedCount(ulong? leavingSteamId = null)
  {
    var connected = ConnectedSteamIds();
    return Queue.Items.Count(steamId => steamId != leavingSteamId && connected.Contains(steamId));
  }

  public static string JoinQueue(CCitadelPlayerController player, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!MatchConfig.IsDuel)
      return "The queue is only open in 1v1 mode.";

    if (!Participants.IsParticipant(player))
      return $"{player.PlayerName} is not playing.";

    var steamId = player.PlayerSteamId;

    if (Queue.PositionOf(steamId) is { } existing)
      return $"{player.PlayerName} is already in the 1v1 queue at position {existing} of {Queue.Count}.";

    var position = Queue.Join(steamId);
    Log.WithMode(mode).Info(player.ToPlayerRef(), "Joined the 1v1 queue Position={Position} Queued={Queued}", position, Queue.Count);

    return $"{player.PlayerName} joined the 1v1 queue at position {position} of {Queue.Count}.";
  }

  public static string LeaveQueue(CCitadelPlayerController player, ExecutionMode mode = ExecutionMode.Clean, bool force = false)
  {
    var steamId = player.PlayerSteamId;

    if (!Queue.Contains(steamId))
      return $"{player.PlayerName} is not in the 1v1 queue.";

    if (IsFighter(steamId) && RiftService.IsRunning)
      return $"{player.PlayerName} is fighting now. Wait for the round to end.";

    if (IsFighter(steamId) && !force)
      return $"{player.PlayerName} fights next round and can't leave during the match.";

    Queue.Leave(steamId);
    DropFighter(steamId);
    Log.WithMode(mode).Info(player.ToPlayerRef(), "Left the 1v1 queue Queued={Queued}", Queue.Count);

    return $"{player.PlayerName} left the 1v1 queue.";
  }

  public static string ResultHeadline(RiftRoundResult result) =>
    KothRule.Winner(Teams, result.Outcome == RiftOutcome.Finished ? result.WinnerTeam : null) is { } winner
      ? $"{NameOf(winner)} wins (streak {Streak})"
      : result.Outcome switch
      {
        RiftOutcome.Tied => "Tie - both stay",
        RiftOutcome.Cancelled => "Round cancelled - both stay",
        RiftOutcome.SpawnTimedOut => "Rift did not spawn - both stay",
        _ => "No winner - both stay"
      };

  public static string NextPairing()
  {
    var fighters = Queue.Front(CompetitorCount);

    return fighters.Count < CompetitorCount
      ? "Waiting for a challenger (/queue)"
      : $"{NameOf(fighters[0])} vs {NameOf(fighters[1])}";
  }

  public static string Copy(CCitadelPlayerController source, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);

    if (!MatchConfig.IsDuel)
      return "Switch to 1v1 first: /match_mode 1v1.";

    if (MatchService.State.IsRunning)
      return "A match is running. Use /match_end first.";

    if (!Participants.IsParticipant(source))
      return $"{source.PlayerName} is not playing.";

    var pawn = source.GetHeroPawn();

    if (pawn == null || !pawn.IsAlive)
      return $"{source.PlayerName} has no live hero to copy.";

    var queued = QueuedCount();

    if (queued < CompetitorCount)
      return $"1v1 needs {CompetitorCount} players in the queue (now {queued}). Players join with /queue.";

    Snapshot = LoadoutService.Capture(pawn, source.PlayerName);

    log.Info(source.ToPlayerRef(), "Snapshot captured Snapshot={Snapshot}", Snapshot.Describe());
    BublockLog.Master.Info("1v1 build copied Hero={Hero} From={From}", Snapshot.Hero, source.PlayerName);

    return $"Copied {HeroBuildCatalog.Default.DisplayName(Snapshot.Hero)} from {source.PlayerName}. {MatchService.Start(timer, mode)}";
  }

  public static string ClearSnapshot(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (MatchService.State.IsRunning)
      return "A match is running. Use /match_end first.";

    if (Snapshot == null)
      return "No 1v1 build is stored.";

    Snapshot = null;
    Log.WithMode(mode).Info("Snapshot cleared");

    if (MatchConfig.IsDuel)
      EnterSetup(timer, mode, announce: true);

    return "1v1 build cleared. Players can switch heroes and build again.";
  }

  public static void BeginMatch(ExecutionMode mode = ExecutionMode.Clean)
  {
    Lock.Clear();
    Teams.Clear();
    Locked = true;
    King = null;
    Streak = 0;
    Streaks.Clear();

    PruneQueue();

    Log.WithMode(mode).Info(
      "1v1 match begins, hero lock on Hero={Hero} Queued={Queued} Next={Next}",
      Snapshot?.Hero.ToString() ?? "-",
      Queue.Count,
      NextPairing());
  }

  public static ulong? RecordResult(RiftRoundResult result, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var winnerTeam = result.Outcome == RiftOutcome.Finished ? result.WinnerTeam : null;
    var winner = KothRule.Winner(Teams, winnerTeam);
    var loser = KothRule.Loser(Teams, winnerTeam);

    if (winner is not { } king || loser is not { } beaten)
    {
      log.Info("1v1 round has no winner, both fighters stay Outcome={Outcome} Next={Next}", result.Outcome, NextPairing());
      return null;
    }

    (King, Streak) = KothRule.Crown(King, Streak, king);
    Streaks.Record(king, Streak);
    Queue.MoveToBack(beaten);

    log.Info(
      "1v1 round won Winner={Winner} Loser={Loser} Streak={Streak} Best={Best} Next={Next}",
      NameOf(king),
      NameOf(beaten),
      Streak,
      Streaks.BestOf(king),
      NextPairing());
    BublockLog.Master.Info("1v1 winner Winner={Winner} Streak={Streak}", NameOf(king), Streak);
    StatsService.RefreshBoards(mode);
    return king;
  }

  public static int PrepareRound(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);

    if (Snapshot == null)
    {
      log.Warn("1v1 round prepared without a copied build");
      return 0;
    }

    PruneQueue();

    var fighterIds = Queue.Front(CompetitorCount);

    foreach (var steamId in Teams.Keys.Where(steamId => !fighterIds.Contains(steamId)).ToList())
      DropFighter(steamId);

    Lock.ClearAllPending();

    if (fighterIds.Count < CompetitorCount)
    {
      log.Info("1v1 round waiting for a challenger Queued={Queued}", Queue.Count);
      return 0;
    }

    var players = Participants.Humans()
      .Where(player => fighterIds.Contains(player.PlayerSteamId))
      .ToList();

    AssignTeams(players, log);

    var applied = 0;

    foreach (var player in players)
    {
      if (Start(player, timer, mode))
        applied++;
      else
        Lock.MarkPending(player.PlayerSteamId);
    }

    log.Info(
      "1v1 round prepared Hero={Hero} Fighters={Fighters} Applied={Applied} Pending={Pending}",
      Snapshot.Hero,
      NextPairing(),
      applied,
      Lock.PendingCount);
    StatsService.RefreshBoards(mode);
    return applied;
  }

  public static bool ApplyPending(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;

    if (!MatchConfig.IsDuel || !Locked || !Lock.IsPending(steamId))
      return false;

    if (!Start(player, timer, mode))
      return false;

    Lock.ClearPending(steamId);
    Log.WithMode(mode).Info(player.ToPlayerRef(), "Pending 1v1 build started after spawn");
    return true;
  }

  public static bool GuardHero(
    CCitadelPlayerController player,
    CCitadelPlayerPawn pawn,
    ITimer timer,
    ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!MatchConfig.IsDuel)
      return false;

    if (IsFighter(player.PlayerSteamId) && Snapshot is { } snapshot)
    {
      Lock.Enforce(
        player,
        pawn,
        snapshot.Hero,
        HeroBuildCatalog.Default.DisplayName(snapshot.Hero),
        Log.WithMode(mode),
        () => Start(player, timer, mode));
    }

    return true;
  }

  public static bool ConsumeEnforcementKill(ulong steamId) => Lock.ConsumeKill(steamId);

  public static int EndMatch(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var competitors = Teams.Count;

    Locked = false;
    Lock.Clear();
    Teams.Clear();
    King = null;
    Streak = 0;
    Streaks.Clear();

    Log.WithMode(mode).Info(
      "1v1 match ended, hero lock off, build and queue kept Competitors={Competitors} Queued={Queued} HasSnapshot={HasSnapshot}",
      competitors,
      Queue.Count,
      HasSnapshot);
    StatsService.RefreshBoards(mode);
    EnterSetup(timer, mode, announce: false);
    return competitors;
  }

  public static int EnterSetup(ITimer timer, ExecutionMode mode = ExecutionMode.Clean, bool announce = false)
  {
    var players = Participants.Humans();

    foreach (var player in players)
      GrantSetup(player, timer, mode, announce);

    Log.WithMode(mode).Info("1v1 setup, free hero switching Players={Players} Gold={Gold}", players.Count, SetupGold);
    return players.Count;
  }

  public static void GrantSetup(
    CCitadelPlayerController player,
    ITimer timer,
    ExecutionMode mode = ExecutionMode.Clean,
    bool announce = false)
  {
    var steamId = player.PlayerSteamId;

    timer.Once(SetupDelaySeconds.Seconds(), () =>
    {
      if (!MatchConfig.IsDuel || MatchService.State.IsRunning)
        return;

      var current = Players.GetAll().FirstOrDefault(candidate => candidate.PlayerSteamId == steamId);
      var pawn = current?.GetHeroPawn();

      if (current == null || pawn == null || !pawn.IsAlive || !Participants.IsParticipant(current))
        return;

      if (pawn.Level < SetupLevel)
      {
        pawn.Level = SetupLevel;
        pawn.ModifyCurrency(ECurrencyType.EGold, 0, ECurrencySource.ECheats, silent: true);
      }

      pawn.SetCurrency(ECurrencyType.EGold, SetupGold);

      if (announce)
        HudService.Announce(current, SetupTitle, SetupDescription, mode);

      Log.WithMode(mode).Debug(
        current.ToPlayerRef(),
        "Setup souls granted Gold={Gold} Level={Level} AP={AP} Unlocks={Unlocks}",
        SetupGold,
        pawn.Level,
        pawn.GetCurrency(ECurrencyType.EAbilityPoints),
        pawn.GetCurrency(ECurrencyType.EAbilityUnlocks));
    });
  }

  public static void Forget(ulong steamId)
  {
    Queue.Leave(steamId);
    DropFighter(steamId);
    Streaks.Forget(steamId);

    if (King == steamId)
    {
      King = null;
      Streak = 0;
    }
  }

  public static void Leave(ExecutionMode mode = ExecutionMode.Clean)
  {
    Snapshot = null;
    Locked = false;
    Lock.Clear();
    Teams.Clear();
    Queue.Clear();
    King = null;
    Streak = 0;
    Streaks.Clear();
    Log.WithMode(mode).Info("Left 1v1 mode, build and queue dropped");
  }

  public static string KingName() => King is { } king ? NameOf(king) : "-";

  public static IReadOnlyList<StreakRow> StreakRows() =>
    StatsBoardText.RankStreaks(Streaks.Best.Select(entry => new StreakRow(NameOf(entry.Key), entry.Value)));

  public static IReadOnlyList<string> DescribeStreaks() =>
    [StatsBoardText.StreakTitle, .. StatsBoardText.StreakLines(StreakRows())];

  public static string StreakSummary()
  {
    var rows = StreakRows();

    return rows.Count == 0
      ? StatsBoardText.NoStreaks
      : "Best streaks: " + string.Join(", ", rows.Select(row => $"{row.Name} {row.Best}"));
  }

  public static IReadOnlyList<string> DescribeQueue()
  {
    if (Queue.Count == 0)
      return ["1v1 queue is empty. Join with /queue."];

    var lines = new List<string> { $"1v1 queue ({Queue.Count}): {NextPairing()}" };
    var position = 0;

    foreach (var steamId in Queue.Items)
    {
      position++;
      var tags = new List<string>();

      if (IsFighter(steamId))
        tags.Add("fighting");

      if (King == steamId)
        tags.Add($"king, streak {Streak}");

      lines.Add($"{position}. {NameOf(steamId)}{(tags.Count == 0 ? "" : $" ({string.Join(", ", tags)})")}");
    }

    return lines;
  }

  public static IReadOnlyList<string> Describe()
  {
    var lines = new List<string>
    {
      $"{MatchConfig.Describe()} | Locked={(Locked ? "yes" : "no")} | Pending={Lock.PendingCount} | Queued={Queue.Count} | King={(King is { } king ? $"{NameOf(king)} x{Streak}" : "-")}",
      Snapshot == null ? "Build: none (players build freely, then /duel_copy <slot>)" : $"Build: {Snapshot.Describe()}"
    };

    if (Snapshot != null)
      lines.Add("Items: " + (Snapshot.Items.Count == 0 ? "none" : string.Join(", ", Snapshot.Items.Select(item => item.Name))));

    foreach (var player in Participants.Humans())
    {
      var steamId = player.PlayerSteamId;
      var team = Teams.TryGetValue(steamId, out var number) ? number : player.TeamNum;
      var hero = player.GetHeroPawn()?.HeroID.ToString() ?? "-";
      var queue = Queue.PositionOf(steamId) is { } position ? $"Queue={position}" : "Queue=-";
      var fighter = IsFighter(steamId) ? " | FIGHTER" : "";
      var pending = Lock.IsPending(steamId) ? " | PENDING" : "";

      lines.Add($"Slot={player.Slot} | {player.PlayerName} | Team={RiftRouletteTeams.Name(team)} | Hero={hero} | {queue}{fighter}{pending}");
    }

    return lines;
  }

  private static void AssignTeams(IReadOnlyList<CCitadelPlayerController> fighters, Logger log)
  {
    if (fighters.Count != CompetitorCount)
      return;

    var keeper = fighters.FirstOrDefault(player => Teams.ContainsKey(player.PlayerSteamId));

    if (keeper == null)
    {
      Teams.Clear();

      var current = fighters.ToDictionary(player => player.PlayerSteamId, player => player.TeamNum);

      foreach (var (steamId, team) in TeamBalance.Even(current, Random.Shared))
        Teams[steamId] = team;
    }
    else
    {
      var challenger = fighters.First(player => player != keeper);
      var keeperTeam = Teams[keeper.PlayerSteamId];

      Teams[challenger.PlayerSteamId] = keeperTeam == RiftRouletteTeams.Sapphire
        ? RiftRouletteTeams.Amber
        : RiftRouletteTeams.Sapphire;
    }

    foreach (var fighter in fighters)
      log.Debug(fighter.ToPlayerRef(), "1v1 fighter Team={Team}", RiftRouletteTeams.Name(Teams[fighter.PlayerSteamId]));
  }

  private static void DropFighter(ulong steamId)
  {
    Teams.Remove(steamId);
    Lock.Forget(steamId);
  }

  private static void PruneQueue()
  {
    var connected = ConnectedSteamIds();
    Queue.RemoveWhere(steamId => !connected.Contains(steamId));
  }

  private static HashSet<ulong> ConnectedSteamIds() =>
    Participants.Humans().Select(player => player.PlayerSteamId).ToHashSet();

  private static string NameOf(ulong steamId) =>
    Players.GetAll().FirstOrDefault(player => player.PlayerSteamId == steamId)?.PlayerName ?? steamId.ToString();

  private static bool Start(CCitadelPlayerController player, ITimer timer, ExecutionMode mode)
  {
    var snapshot = Snapshot;
    var pawn = player.GetHeroPawn();

    if (snapshot == null)
      return false;

    if (pawn == null || !pawn.IsAlive)
    {
      Log.WithMode(mode).Debug(player.ToPlayerRef(), "1v1 build deferred, player dead Hero={Hero}", snapshot.Hero);
      return false;
    }

    var steamId = player.PlayerSteamId;

    if (Teams.TryGetValue(steamId, out var team) && player.TeamNum != team)
      player.ChangeTeam(team);

    Lock.Unapply(steamId);

    return LoadoutService.SwapSnapshot(
      player,
      snapshot,
      timer,
      gold: 0,
      mode,
      (_, _) =>
      {
        if (Locked && Snapshot == snapshot)
          Lock.MarkApplied(steamId);
      });
  }
}
