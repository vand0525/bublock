using Bublock.Modules.Hud;
using Bublock.Modules.Loadout;
using Bublock.Modules.Queue;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Balance;
using RiftRoulette.GameLoop;
using RiftRoulette.Lobby;
using RiftRoulette.RandomMode;
using RiftRoulette.Round;
using RiftRoulette.Stats;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.Mirror;

public static class MirrorModeService
{
  public const double JoinerFallbackSeconds = 2.0;

  public const string ClearWord = "clear";

  private static readonly Logger Log = BublockLog.For("Mirror");

  private static readonly Dictionary<ulong, int> Teams = [];

  private static readonly HashSet<ulong> Fighters = [];

  private static readonly Dictionary<ulong, int> Values = [];

  private static readonly HeroLock Lock = new();

  private static readonly PlayerQueue BenchRotation = new();

  private static ulong? _benched;

  private static ulong? _returning;

  private static int? _benchRound;

  private static bool _buildsAnnounced;

  private static MirrorChoice? _current;

  // Optional build groups pick one item at random; drawn once per choice so every fighter shops the same list.
  private static IReadOnlyList<string>? _itemOrder;

  private static Heroes? _lastHero;

  private static int _version;

  // Rolled heroes whose swap failed this match (the server could not spawn them); retried next match.
  private static readonly HashSet<Heroes> FailedHeroes = [];

  // Pins outlive matches, mode changes and map reloads; only a DLL load or an admin clears them.
  public static MirrorPins Pins { get; private set; } = MirrorPins.None;

  public static MirrorChoice? Current => _current;

  public static int PendingCount => Lock.PendingCount;

  public static int FighterCount => Fighters.Count;

  public static bool IsFighter(ulong steamId) => Fighters.Contains(steamId);

  public static void BeginMatch(ExecutionMode mode = ExecutionMode.Clean)
  {
    Clear();

    var current = Humans().ToDictionary(player => player.PlayerSteamId, player => player.TeamNum);

    foreach (var (steamId, team) in TeamBalance.Even(current, Random.Shared))
      Teams[steamId] = team;

    Log.WithMode(mode).Info(
      "Teams balanced Sapphire={Sapphire} Amber={Amber} Pins={Pins}",
      Teams.Values.Count(team => team == RiftRouletteTeams.Sapphire),
      Teams.Values.Count(team => team == RiftRouletteTeams.Amber),
      DescribePins());
  }

  public static int PrepareRound(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var players = Humans();
    var connected = players.Select(player => player.PlayerSteamId).ToHashSet();

    foreach (var gone in Teams.Keys.Where(steamId => !connected.Contains(steamId)).ToList())
      Teams.Remove(gone);

    foreach (var player in players.Where(player => !Teams.ContainsKey(player.PlayerSteamId)))
    {
      Teams[player.PlayerSteamId] = TeamBalance.SmallerTeam(Teams.Values, Random.Shared);
      log.Info(player.ToPlayerRef(), "Late joiner placed Team={Team}", RiftRouletteTeams.Name(Teams[player.PlayerSteamId]));
    }

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

    BalanceService.TryBalance(fighters, mode);

    foreach (var (steamId, team) in fighters)
      Teams[steamId] = team;

    if (_benched is { } benchedId && Find(benchedId) is { } benchedPlayer)
      log.Info(benchedPlayer.ToPlayerRef(), "Sitting out this round Players={Players}", connected.Count);

    Fighters.Clear();
    RoundHeroes.Clear();

    foreach (var steamId in fighters.Keys)
      Fighters.Add(steamId);

    _buildsAnnounced = false;
    return ApplyChoice(timer, mode, roll: true);
  }

  // Resolves the shared pair once, then gives that exact hero and build to every fighter.
  private static int ApplyChoice(ITimer timer, ExecutionMode mode, bool roll)
  {
    var log = Log.WithMode(mode);
    var catalog = HeroBuildCatalog.Default;

    var current = _current;

    if (roll || current == null || !MatchesPins(current))
    {
      current = MirrorPick.Resolve(Pins, Pool(catalog), hero => catalog.BuildsFor(hero).Count, _lastHero, Random.Shared);

      if (current == null)
      {
        log.Warn("Mirror pick failed, no hero has stored builds Pins={Pins}", DescribePins());
        return 0;
      }

      if (Pins.Hero is { } pinned && pinned != current.Hero)
        log.Warn("Pinned hero has no stored builds, rolled instead Pinned={Pinned} Hero={Hero}", pinned, current.Hero);

      _current = current;
      _lastHero = current.Hero;
      _itemOrder = null;
    }

    var build = BuildOf(current);
    var itemOrder = _itemOrder ??= LoadoutPlanner.ItemOrder(build, Random.Shared);
    _version++;
    Values.Clear();
    Lock.ClearAllPending();

    var swapped = 0;
    var players = Humans().Where(player => Fighters.Contains(player.PlayerSteamId)).ToList();

    foreach (var player in players)
    {
      Assign(player.PlayerSteamId, current.Hero);

      if (Start(player, timer, mode))
        swapped++;
      else
        Lock.MarkPending(player.PlayerSteamId);
    }

    log.Info(
      "Round prepared Hero={Hero} Build={Build} BuildId={BuildId} Slot={Slot} Pins={Pins} Fighters={Fighters} Swapped={Swapped} Pending={Pending} ItemOrder={ItemOrder}",
      current.Hero,
      build.Name,
      build.BuildId,
      current.BuildIndex + 1,
      DescribePins(),
      players.Count,
      swapped,
      Lock.PendingCount,
      string.Join(",", itemOrder));
    StatsService.RefreshBoards(mode);
    return swapped;
  }

  private static IReadOnlyList<Heroes> Pool(HeroBuildCatalog catalog)
  {
    var working = catalog.Heroes.Where(hero => !FailedHeroes.Contains(hero)).ToList();
    return working.Count > 0 ? working : catalog.Heroes;
  }

  // Every fighter fails at once; the first callback rerolls and bumps _version, so the rest are ignored.
  private static void OnSwapFailed(MirrorChoice failed, int version, ITimer timer, ExecutionMode mode)
  {
    var log = Log.WithMode(mode);

    if (version != _version || _current != failed || !MatchService.State.IsRunning)
      return;

    if (Pins.Hero == failed.Hero)
    {
      log.Warn("Pinned mirror hero did not spawn, kept by the pin Hero={Hero}", failed.Hero);
      return;
    }

    FailedHeroes.Add(failed.Hero);
    log.Warn("Mirror hero did not spawn, rerolled for everyone Failed={Failed}", failed.Hero);
    ApplyChoice(timer, mode, roll: true);
  }

  private static bool MatchesPins(MirrorChoice choice) =>
    (Pins.Hero == null || Pins.Hero == choice.Hero) && (Pins.Slot == null || Pins.Slot == choice.BuildIndex + 1);

  private static HeroBuild BuildOf(MirrorChoice choice) => HeroBuildCatalog.Default.BuildsFor(choice.Hero)[choice.BuildIndex];

  private static void Assign(ulong steamId, Heroes hero)
  {
    Fighters.Add(steamId);
    Lock.Unapply(steamId);
    RoundHeroes.Set(steamId, hero);
  }

  public static string PinHero(string heroText, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var text = heroText.Trim();
    var catalog = HeroBuildCatalog.Default;

    if (text.Length == 0)
      return DescribePins();

    if (string.Equals(text, ClearWord, StringComparison.OrdinalIgnoreCase))
    {
      Pins = MirrorPins.None;
      Log.WithMode(mode).Info("Mirror pins cleared");
      return $"Mirror pins cleared: hero and build roll every round. {AfterPinChange(timer, mode)}";
    }

    if (!TryParseHero(catalog, text, out var hero))
      return $"No hero called '{text}' with stored builds. Use the hero's name, for example /mirror_hero haze.";

    var count = catalog.BuildsFor(hero).Count;
    Pins = Pins.PinHero(hero, count);
    Log.WithMode(mode).Info("Mirror hero pinned Hero={Hero} Slot={Slot} Builds={Builds}", hero, Pins.Slot, count);

    return $"Mirror hero pinned: {catalog.DisplayName(hero)}. {DescribeBuildPin()} {AfterPinChange(timer, mode)}";
  }

  public static string PinBuild(string slotText, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var text = slotText.Trim();
    var catalog = HeroBuildCatalog.Default;

    if (Pins.Hero is not { } hero)
      return "Pin a hero first: /mirror_hero <hero>. The builds belong to that hero.";

    var builds = catalog.BuildsFor(hero);

    if (text.Length == 0)
      return $"{catalog.DisplayName(hero)} builds: {ListBuilds(builds)}. {DescribeBuildPin()}";

    if (string.Equals(text, ClearWord, StringComparison.OrdinalIgnoreCase))
    {
      Pins = Pins.ClearSlot();
      Log.WithMode(mode).Info("Mirror build pin cleared Hero={Hero}", hero);
      return $"Mirror build pin cleared. {DescribeBuildPin()} {AfterPinChange(timer, mode)}";
    }

    if (!int.TryParse(text, out var slot) || !Pins.TryPinSlot(slot, builds.Count, out var pinned))
      return $"Build must be 1 to {builds.Count} or clear: {ListBuilds(builds)}.";

    Pins = pinned;
    Log.WithMode(mode).Info("Mirror build pinned Hero={Hero} Slot={Slot} Build={Build}", hero, slot, builds[slot - 1].Name);
    return $"Mirror build pinned: {DescribeBuildPin()} {AfterPinChange(timer, mode)}";
  }

  private static string AfterPinChange(ITimer timer, ExecutionMode mode)
  {
    if (!MatchConfig.IsMirror)
      return "Saved for when mirror mode is on.";

    if (!MatchService.State.IsRunning)
      return "Used when the match starts.";

    if (MatchService.State.Phase != MatchPhase.Intermission)
      return "Used from the next intermission.";

    var swapped = ApplyChoice(timer, mode, roll: false);
    return $"Applied now ({swapped} swapped, {Lock.PendingCount} pending).";
  }

  private static string DescribeBuildPin()
  {
    if (Pins.Hero is not { } hero)
      return "Build: random each round.";

    var builds = HeroBuildCatalog.Default.BuildsFor(hero);

    return Pins.Slot is { } slot
      ? $"Build {slot}: {builds[slot - 1].Name}."
      : $"Build: one of its {builds.Count} builds each round, the same for everyone (/mirror_build 1-{builds.Count} to pin).";
  }

  private static string ListBuilds(IReadOnlyList<HeroBuild> builds) =>
    string.Join(", ", builds.Select((build, index) => $"{index + 1} {build.Name}"));

  private static bool TryParseHero(HeroBuildCatalog catalog, string text, out Heroes hero) =>
    (catalog.TryParseHero(text, out hero) || catalog.TryParseHero(text.Replace(" ", ""), out hero))
    && catalog.BuildsFor(hero).Count > 0;

  // During an intermission the fighters stay even: a joiner fills an odd gap, swaps in with the bench player, or sits out.
  public static void AddJoiner(CCitadelPlayerController player, int team, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var steamId = player.PlayerSteamId;

    Teams[steamId] = team;
    BenchRotation.Join(steamId);

    if (MatchService.State.Phase != MatchPhase.Intermission || _current == null)
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
      HudService.Announce(player, RandomModeService.SitOutTitle, RandomModeService.SitOutDescription, mode);
  }

  // Called before the leaver is removed. In an intermission the bench player takes the leaver's team.
  public static void OnLeave(ulong steamId, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var wasFighter = Fighters.Contains(steamId);
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
    Log.WithMode(mode).Info(benched.ToPlayerRef(), "Subbed in for a player who left Team={Team}", RiftRouletteTeams.Name(team));
  }

  private static void AssignLate(CCitadelPlayerController player, ITimer timer, ExecutionMode mode)
  {
    if (_current is not { } current)
      return;

    var steamId = player.PlayerSteamId;
    Assign(steamId, current.Hero);
    Lock.MarkPending(steamId);
    Log.WithMode(mode).Info(player.ToPlayerRef(), "Late mirror hero assigned, pending spawn Hero={Hero} Team={Team}", current.Hero, RiftRouletteTeams.Name(Teams[steamId]));

    timer.Once(JoinerFallbackSeconds.Seconds(), () =>
    {
      if (Find(steamId) is { } current)
        ApplyPending(current, timer, mode);
    });
  }

  private static List<int> FighterTeams()
  {
    var connected = Humans().Select(player => player.PlayerSteamId).ToHashSet();
    return Fighters.Where(connected.Contains).Select(steamId => Teams[steamId]).ToList();
  }

  private static CCitadelPlayerController? Find(ulong steamId) =>
    Players.GetAll().FirstOrDefault(player => player.PlayerSteamId == steamId);

  public static bool ApplyPending(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;

    if (!MatchConfig.IsMirror || !Lock.IsPending(steamId) || !Fighters.Contains(steamId) || _current is not { } current)
      return false;

    if (!Start(player, timer, mode))
      return false;

    Lock.ClearPending(steamId);
    Log.WithMode(mode).Info(player.ToPlayerRef(), "Pending loadout started after spawn Hero={Hero}", current.Hero);
    return true;
  }

  public static bool GuardHero(
    CCitadelPlayerController player,
    CCitadelPlayerPawn pawn,
    ITimer timer,
    ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;

    if (!MatchConfig.IsMirror || !Fighters.Contains(steamId) || _current is not { } current)
      return false;

    Lock.Enforce(
      player,
      pawn,
      current.Hero,
      HeroBuildCatalog.Default.DisplayName(current.Hero),
      Log.WithMode(mode),
      () => Start(player, timer, mode));

    return true;
  }

  public static bool ConsumeEnforcementKill(ulong steamId) => Lock.ConsumeKill(steamId);

  public static void Forget(ulong steamId)
  {
    Teams.Remove(steamId);
    Fighters.Remove(steamId);
    Values.Remove(steamId);
    Lock.Forget(steamId);
    BenchRotation.Leave(steamId);

    if (_benched == steamId)
      _benched = null;
  }

  private static bool Start(CCitadelPlayerController player, ITimer timer, ExecutionMode mode)
  {
    if (_current is not { } choice)
      return false;

    var pawn = player.GetHeroPawn();

    if (pawn == null || !pawn.IsAlive)
    {
      Log.WithMode(mode).Debug(player.ToPlayerRef(), "Swap deferred, player dead Hero={Hero}", choice.Hero);
      return false;
    }

    var steamId = player.PlayerSteamId;

    if (Teams.TryGetValue(steamId, out var team) && player.TeamNum != team)
      player.ChangeTeam(team);

    Lock.Unapply(steamId);
    var version = _version;

    return LoadoutService.Swap(
      player,
      choice.Hero,
      BuildOf(choice),
      timer,
      RandomModeService.Options with { ItemOrder = _itemOrder },
      mode,
      (current, result) =>
      {
        if (version != _version || !Fighters.Contains(steamId))
          return;

        Lock.MarkApplied(steamId);
        Values[steamId] = result.Value;

        if (_buildsAnnounced)
          AnnounceBuild(current, mode);
      },
      _ => OnSwapFailed(choice, version, timer, mode));
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
        HudService.Announce(player, RandomModeService.SitOutTitle, RandomModeService.SitOutDescription, mode);
        shown++;
      }
    }

    return shown;
  }

  private static bool AnnounceBuild(CCitadelPlayerController player, ExecutionMode mode)
  {
    if (_current is not { } choice || !Fighters.Contains(player.PlayerSteamId) || !Values.TryGetValue(player.PlayerSteamId, out var souls))
      return false;

    HudService.Announce(
      player,
      HeroBuildCatalog.Default.DisplayName(choice.Hero),
      RandomModeService.BuildDescription(BuildOf(choice).Name, souls),
      mode);

    return true;
  }

  public static int EndMatch(ExecutionMode mode = ExecutionMode.Clean)
  {
    var reset = 0;

    foreach (var player in Players.GetAll())
    {
      var pawn = player.GetHeroPawn();

      if (pawn == null || !pawn.IsAlive || !Fighters.Contains(player.PlayerSteamId))
        continue;

      pawn.ResetHero();
      reset++;
    }

    Log.WithMode(mode).Info("Mirror match ended HeroesReset={HeroesReset} Pins={Pins}", reset, DescribePins());
    Clear();
    return reset;
  }

  public static string DescribePins()
  {
    if (Pins.Hero is not { } hero)
      return "Mirror hero: random each round. Build: random each round.";

    return $"Mirror hero: {HeroBuildCatalog.Default.DisplayName(hero)} (pinned). {DescribeBuildPin()}";
  }

  public static IReadOnlyList<string> Describe()
  {
    var catalog = HeroBuildCatalog.Default;
    var now = _current is { } choice
      ? $"{catalog.DisplayName(choice.Hero)} | Build {choice.BuildIndex + 1}={BuildOf(choice).Name} ({BuildOf(choice).BuildId})"
      : "none yet";
    var lines = new List<string>
    {
      $"{MatchConfig.Describe()} | Fighters={Fighters.Count} | Pending={Lock.PendingCount} | " +
      $"Bench={(_benched is { } benched ? Find(benched)?.PlayerName ?? benched.ToString() : "none")}",
      DescribePins(),
      $"Current: {now}"
    };

    if (Pins.Hero is { } hero)
      lines.Add($"{catalog.DisplayName(hero)} builds: {ListBuilds(catalog.BuildsFor(hero))}");

    if (FailedHeroes.Count > 0)
      lines.Add($"Swap failed, off this match: {string.Join(", ", FailedHeroes.Select(catalog.DisplayName).Order())}");

    foreach (var player in Players.GetAll())
    {
      var steamId = player.PlayerSteamId;
      var team = Teams.TryGetValue(steamId, out var number) ? RiftRouletteTeams.Name(number) : "-";
      var role = Fighters.Contains(steamId) ? "fighting" : _benched == steamId ? "SITTING OUT" : "-";
      var pending = Lock.IsPending(steamId) ? " | PENDING" : "";

      lines.Add($"Slot={player.Slot} | {player.PlayerName} | Team={team} | {role}{pending}");
    }

    return lines;
  }

  private static List<CCitadelPlayerController> Humans() => Participants.Humans();

  private static void Clear()
  {
    Teams.Clear();
    Fighters.Clear();
    Values.Clear();
    _buildsAnnounced = false;
    Lock.Clear();
    BenchRotation.Clear();
    _benched = null;
    _returning = null;
    _benchRound = null;
    _current = null;
    _itemOrder = null;
    _lastHero = null;
    _version++;
    FailedHeroes.Clear();
  }
}
