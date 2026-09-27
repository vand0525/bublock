using Bublock.Modules.WorldText;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Betting;
using RiftRoulette.Duel;
using RiftRoulette.GameLoop;
using RiftRoulette.Lobby;
using RiftRoulette.RandomMode;
using RiftRoulette.Round;
using RiftRoulette.Stats;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.Draft;

public static class DraftService
{
  private const int LobbyTeam = RiftRouletteTeams.Amber;

  private const int StartingGold = 25000;

  public const string RandomModeReply = "Heroes are random this match - you get a new hero and build every round.";

  public const string DuelModeReply = "1v1 mode - pick a hero from the hero menu; the admin copies one build onto both players.";

  public const string WelcomeNote = "MY BAD BOMER AND TIMEBUCKET\nI DIDNT MEAN TO CRASH IT";

  public static string NoDraftReply => MatchConfig.IsDuel ? DuelModeReply : RandomModeReply;

  private static readonly Logger Log = BublockLog.For("Draft");

  public static bool CanChangeHero(CCitadelPlayerController player)
  {
    var pawn = player.GetHeroPawn();

    return pawn != null && pawn.IsAlive;
  }

  public static string Pick(
    CCitadelPlayerController player,
    string heroName,
    ITimer timer,
    ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var who = player.ToPlayerRef();

    if (!MatchConfig.UsesDraft)
    {
      log.Debug(who, "Pick refused, draft off HeroName={HeroName} Mode={Mode}", heroName, MatchConfig.HeroMode);
      return NoDraftReply;
    }

    if (!CanChangeHero(player))
    {
      log.Debug(who, "Pick refused while dead HeroName={HeroName}", heroName);
      return "You cannot select a hero while dead.";
    }

    if (!Enum.TryParse<Heroes>(heroName, true, out var hero))
    {
      log.Info(who, "Unknown hero HeroName={HeroName}", heroName);
      return $"Rift Roulette: Unknown hero '{heroName}'.";
    }

    if (DraftState.HasPick(player.PlayerSteamId))
    {
      log.Debug(who, "Pick refused, already picked Hero={Hero}", hero);
      return "You have already selected a hero.";
    }

    if (DraftState.IsSelected(hero))
    {
      log.Info(who, "Hero already selected Hero={Hero}", hero);
      return $"{hero} has already been drafted. Choose another hero.";
    }

    var team = DraftPools.TeamOf(hero);

    if (team == 0)
    {
      log.Info(who, "Hero not in draft Hero={Hero}", hero);
      return $"{hero} is not available in this draft.";
    }

    DraftState.Add(player.PlayerSteamId, hero);

    player.ChangeTeam(team);
    player.SelectHero(hero);

    timer.NextTick(() => GiveStartingProgression(player, mode));

    RedrawBoards(mode);

    log.Info(who, "Selected hero Hero={Hero} Team={Team}", hero, RiftRouletteTeams.Name(team));
    return $"You selected {hero}.";
  }

  public static void GiveStartingProgression(CCitadelPlayerController player, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!DraftState.HasPick(player.PlayerSteamId))
      return;

    var pawn = player.GetHeroPawn();

    if (pawn == null)
      return;

    pawn.SetCurrency(ECurrencyType.EGold, StartingGold);
    Log.WithMode(mode).Info(player.ToPlayerRef(), "Gave starting progression Gold={Gold}", StartingGold);
  }

  public static string Unpick(
    CCitadelPlayerController player,
    ITimer timer,
    ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var who = player.ToPlayerRef();

    if (!MatchConfig.UsesDraft)
    {
      log.Debug(who, "Unpick refused, draft off Mode={Mode}", MatchConfig.HeroMode);
      return NoDraftReply;
    }

    if (!DraftState.TryGetPick(player.PlayerSteamId, out var hero))
      return "You haven't selected a hero.";

    if (!CanChangeHero(player))
    {
      log.Info(who, "Blocked unpick while dead Hero={Hero}", hero);
      return "You cannot unselect your hero while dead.";
    }

    var pawn = player.GetHeroPawn();

    if (pawn != null)
      ClearProgression(pawn);

    DraftState.Release(player.PlayerSteamId, out _);

    player.ChangeTeam(LobbyTeam, true);
    player.SelectHero(Heroes.Skyrunner);

    timer.NextTick(() => WatchSpot.SendUp(player, mode));

    RedrawBoards(mode);

    log.Info(who, "Unselected hero Hero={Hero}", hero);
    return $"You unselected {hero}.";
  }

  public static int Reset(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var reset = 0;

    DraftState.Clear();

    foreach (var player in Participants.Humans())
    {
      var pawn = player.GetHeroPawn();

      if (pawn == null)
        continue;

      if (!pawn.IsAlive)
      {
        log.Debug(player.ToPlayerRef(), "Reset skipped dead player LifeState={LifeState}", pawn.LifeState);
        continue;
      }

      ClearProgression(pawn);

      if (MatchConfig.UsesDraft)
        player.ChangeTeam(LobbyTeam, true);

      player.SelectHero(Heroes.Skyrunner);

      timer.NextTick(() => WatchSpot.SendUp(player, mode));
      reset++;
    }

    RedrawBoards(mode);

    log.Info("Draft reset PlayersReset={PlayersReset}", reset);
    return reset;
  }

  public static void EnforceHero(CCitadelPlayerController player, CCitadelPlayerPawn pawn, ITimer timer)
  {
    if (!Participants.IsParticipant(player))
      return;

    if (DuelService.GuardHero(player, pawn, timer))
      return;

    if (RandomModeService.GuardHero(player, pawn, timer))
      return;

    var expectedHero = DraftState.TryGetPick(player.PlayerSteamId, out var selectedHero)
      ? selectedHero
      : Heroes.Skyrunner;

    if (pawn.HeroID != expectedHero)
    {
      if (!pawn.IsAlive)
      {
        Log.Debug(
          player.ToPlayerRef(),
          "Hero enforcement skipped while dead Current={Current} Expected={Expected} LifeState={LifeState}",
          pawn.HeroID,
          expectedHero,
          pawn.LifeState);

        return;
      }

      player.SelectHero(expectedHero);
      return;
    }

    if (!DraftState.HasPick(player.PlayerSteamId))
      ClearProgression(pawn);
  }

  public static void RedrawBoards(ExecutionMode mode = ExecutionMode.Clean)
  {
    WorldTextService.ClearAll(mode);

    WorldTextService.Create("draft.welcome", BoardLayout.Welcome("RIFT ROULETTE"), mode);

    if (WelcomeNote.Length > 0)
      WorldTextService.Create("draft.note", BoardLayout.Note(WelcomeNote), mode);

    if (!MatchConfig.UsesDraft)
    {
      StatsService.RefreshBoards(mode);
      BettingService.RefreshBoard(mode);
      Log.WithMode(mode).Debug("Boards redrawn, draft off (welcome + stats + betting) Mode={Mode}", MatchConfig.HeroMode);
      return;
    }

    WorldTextService.Create(
      "draft.sapphire",
      BoardLayout.Side(RiftRouletteTeams.Sapphire, DraftBoardText.TeamBoard("SAPPHIRE", DraftPools.Sapphire, DraftState.IsSelected)),
      mode);

    WorldTextService.Create(
      "draft.amber",
      BoardLayout.Side(RiftRouletteTeams.Amber, DraftBoardText.TeamBoard("AMBER", DraftPools.Amber, DraftState.IsSelected)),
      mode);

    Log.WithMode(mode).Debug("Boards redrawn");
  }

  public static string DescribePicks()
  {
    var picks = DraftState.SelectedHeroes.Select(hero => $"{hero} ({Picker(hero)})").ToList();

    return picks.Count == 0 ? "Picks: none" : "Picks: " + string.Join(", ", picks);
  }

  public static IReadOnlyList<string> DescribeHeroes() =>
    !MatchConfig.UsesDraft ? [NoDraftReply] : DescribePools();

  public static IReadOnlyList<string> DescribePools() =>
  [
    DraftBoardText.PoolLine("Sapphire", DraftPools.Sapphire, DraftState.IsSelected),
    DraftBoardText.PoolLine("Amber", DraftPools.Amber, DraftState.IsSelected)
  ];

  public static IReadOnlyList<string> DescribeDraft()
  {
    var lines = new List<string>(DescribePools());
    var picks = DraftState.AllPicks.ToList();

    lines.Add($"{picks.Count} pick(s)");

    foreach (var (steamId, hero) in picks)
    {
      var player = FindPlayer(steamId);
      var holder = player == null ? "(offline)" : $"{player.PlayerName} | slot {player.Slot}";

      lines.Add($"{hero} | {holder} | SteamID={steamId}");
    }

    return lines;
  }

  private static string Picker(Heroes hero)
  {
    foreach (var (steamId, picked) in DraftState.AllPicks)
    {
      if (picked != hero)
        continue;

      var player = FindPlayer(steamId);
      return player == null ? "offline" : $"{player.PlayerName}, slot {player.Slot}";
    }

    return "offline";
  }

  private static CCitadelPlayerController? FindPlayer(ulong steamId) =>
    Players.GetAll().FirstOrDefault(player => player.PlayerSteamId == steamId);

  private static void ClearProgression(CCitadelPlayerPawn pawn)
  {
    pawn.SetCurrency(ECurrencyType.EGold, 0);
    pawn.SetCurrency(ECurrencyType.EAbilityPoints, 0);
    pawn.Level = 0;
  }
}
