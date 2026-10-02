using Bublock.Modules.Hud;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Balance;
using RiftRoulette.Betting;
using RiftRoulette.Boards;
using RiftRoulette.Lobby;
using RiftRoulette.Mirror;
using RiftRoulette.RandomMode;
using RiftRoulette.Rift;
using RiftRoulette.Round;
using RiftRoulette.Stats;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.GameLoop;

public static class MatchService
{
  public const int DefaultIntermissionSeconds = 5;
  public const int MinIntermissionSeconds = 5;
  public const int MaxIntermissionSeconds = 120;

  private const int FinalCountdownSeconds = 3;

  public const int BuildBannerDelaySeconds = 3;

  private static readonly Logger Log = BublockLog.For("Match");

  private static ITimer? _timer;
  private static IHandle? _countdown;
  private static IHandle? _finalCountdown;
  private static IHandle? _buildBanner;
  private static IHandle? _bettingClose;

  public static MatchState State { get; } = new();

  public static int IntermissionSeconds { get; private set; } = DefaultIntermissionSeconds;

  public static string Start(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);

    if (State.IsRunning)
      return "A match is already running. Use /match_end first.";

    if (RiftService.IsRunning)
      return $"A rift is running (Phase={RiftService.Phase}). Wait for it to end or use /rift_cancel.";

    _timer = timer;
    State.Start();
    StatsService.Reset(mode);
    BalanceService.Reset(mode);
    BettingService.Reset(mode);

    if (MatchConfig.IsRandom)
      RandomModeService.BeginMatch(mode);
    else if (MatchConfig.IsMirror)
      MirrorModeService.BeginMatch(mode);

    log.Info("Match started {Config} IntermissionSeconds={IntermissionSeconds}", MatchConfig.Describe(), IntermissionSeconds);
    BublockLog.Master.Info("Match started {Config}", MatchConfig.Describe());

    HudService.AnnounceAll("Match starting", $"Round 1 in {IntermissionSeconds}s", mode);

    ScheduleNextRound(mode);
    MatchProbe.Snapshot("match-start");

    return $"Match started. Round 1 in {IntermissionSeconds}s.";
  }

  public static string End(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);

    if (!State.IsRunning)
      return "No match is running.";

    var score = State.FormatScore();
    var rounds = State.Round;

    CancelCountdown();
    State.Reset();

    if (RiftService.IsRunning)
      log.Info("Cancelling running rift for match end Result={Result}", RoundFlow.CancelRound(timer, mode));

    if (MatchConfig.IsRandom)
    {
      BettingService.EndMatch(mode);
      RandomModeService.EndMatch(mode);
    }
    else if (MatchConfig.IsMirror)
    {
      MirrorModeService.EndMatch(mode);
    }

    var returned = LobbyHeroes.ReturnAll(timer, mode);
    BoardService.Redraw(mode);

    HudService.AnnounceAll("Match over", score, mode);

    log.Info("Match ended Score={Score} Rounds={Rounds} Returned={Returned}", score, rounds, returned);
    BublockLog.Master.Info("Match ended Score={Score} Rounds={Rounds}", score, rounds);

    return $"Match ended after {rounds} round(s). Final: {score}. {returned} player(s) returned to the lobby.";
  }

  public static void OnRoundEnded(RiftRoundResult result, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!State.IsRunning)
      return;

    var log = Log.WithMode(mode);
    var pointTo = State.Apply(result);
    var score = State.FormatScore();

    BalanceService.RecordRound(pointTo, mode);
    StatsService.SetRounds(State.Sapphire, State.Amber, mode);
    BettingService.OnRoundEnded(pointTo, mode);

    if (result.Outcome == RiftOutcome.Finished && pointTo == null)
      log.Warn("Rift finished but the winner team is unknown WinnerTeam={WinnerTeam}", result.WinnerTeam);

    log.Info(
      "Round scored Round={Round} Outcome={Outcome} WinnerTeam={WinnerTeam} PointTo={PointTo} Score={Score}",
      State.Round,
      result.Outcome,
      result.WinnerTeam,
      pointTo,
      score);

    BublockLog.Master.Info("Round {Round} result Outcome={Outcome} Score={Score}", State.Round, result.Outcome, score);
    HudService.AnnounceAll(score, MatchState.DescribeResult(result, pointTo), mode);

    if (MapRefreshService.TryBegin(_timer!, mode))
      return;

    ScheduleNextRound(mode);
  }

  public static bool SetIntermission(int seconds, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (seconds < MinIntermissionSeconds || seconds > MaxIntermissionSeconds)
      return false;

    IntermissionSeconds = seconds;
    Log.WithMode(mode).Info("Intermission set Seconds={Seconds}", seconds);
    return true;
  }

  public static string SetHeroMode(HeroMode heroMode, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var name = heroMode.ToString().ToLowerInvariant();

    if (State.IsRunning)
      return "A match is running. Use /match_end first.";

    if (MatchConfig.HeroMode == heroMode)
      return $"Mode is already {name}.";

    MatchConfig.SetHeroMode(heroMode);
    var returned = LobbyHeroes.ReturnAll(timer, mode);
    BoardService.Redraw(mode);

    var (title, description) = ModeBanner(heroMode);
    HudService.AnnounceAll(title, description, mode);

    Log.WithMode(mode).Info("Hero mode set Mode={Mode} Returned={Returned}", name, returned);
    BublockLog.Master.Info("Hero mode set Mode={Mode}", name);

    return $"Mode set to {name}. {returned} player(s) returned to the lobby.";
  }

  public static string SetFormat(MatchFormat format, ExecutionMode mode = ExecutionMode.Clean)
  {
    var name = format.ToString().ToLowerInvariant();

    if (State.IsRunning)
      return "A match is running. Use /match_end first.";

    MatchConfig.SetFormat(format);
    Log.WithMode(mode).Info("Match format set Format={Format}", name);

    return $"Format set to {name}.";
  }

  public static IReadOnlyList<string> DescribeConfig() =>
  [
    MatchConfig.Describe(),
    $"Modes={MatchConfig.Names<HeroMode>()} | Formats={MatchConfig.Names<MatchFormat>()} | Intermission={IntermissionSeconds}s"
  ];

  public static IReadOnlyList<string> DescribeMatch() =>
  [
    $"Phase={State.Phase} | Round={State.Round} | Score={State.FormatScore()} | Ties={State.Ties} | Auto={(AutoStartService.Enabled ? "on" : "off")}",
    $"{MatchConfig.Describe()} | Intermission={IntermissionSeconds}s | Rift={RiftService.Phase} | NextSide={RiftSides.Name(RiftService.NextSide)}"
  ];

  public static IReadOnlyList<string> DescribeScore() =>
    State.IsRunning
      ? [$"Round {State.Round}: {State.FormatScore()} (ties {State.Ties})"]
      : ["No match is running."];

  private static void ScheduleNextRound(ExecutionMode mode, bool prepareHeroes = true)
  {
    CancelCountdown();
    State.EnterIntermission();

    var timer = _timer!;
    var seconds = IntermissionSeconds;

    FlexSlots.UnlockAll(mode);

    if (prepareHeroes && MatchConfig.IsRandom)
      RandomModeService.PrepareRound(timer, mode);
    else if (prepareHeroes && MatchConfig.IsMirror)
      MirrorModeService.PrepareRound(timer, mode);

    BettingService.Open(mode);

    if (seconds > FinalCountdownSeconds)
    {
      _finalCountdown = timer.Once((seconds - FinalCountdownSeconds).Seconds(), () => AnnounceFinalCountdown(mode));
    }

    if (prepareHeroes)
    {
      var round = State.Round + 1;

      if (seconds - FinalCountdownSeconds > BuildBannerDelaySeconds)
        _buildBanner = timer.Once(BuildBannerDelaySeconds.Seconds(), () => AnnounceBuilds(round, mode));
      else
        AnnounceHeroBuilds(mode);
    }

    _countdown = timer.Once(seconds.Seconds(), () => StartRound(mode));

    Log.WithMode(mode).Debug("Next round scheduled Round={Round} Seconds={Seconds}", State.Round + 1, seconds);
  }

  private static void StartRound(ExecutionMode mode)
  {
    var log = Log.WithMode(mode);

    _countdown = null;
    _finalCountdown = null;

    if (State.Phase != MatchPhase.Intermission)
      return;

    if (RiftService.IsRunning)
    {
      log.Warn("Round start skipped, a rift is already running Phase={Phase}", RiftService.Phase);
      return;
    }

    var side = RiftService.NextSide;
    var reply = RoundFlow.RunRound(_timer!, mode);

    if (!RiftService.IsRunning)
    {
      log.Warn("Round failed to start, retrying after the intermission Reply={Reply}", reply);
      ScheduleNextRound(mode, prepareHeroes: false);
      return;
    }

    State.BeginRound();

    var round = State.Round;
    _bettingClose = _timer!.Once(BettingService.LingerSeconds.Seconds(), () => CloseBetting(round, mode));

    log.Info("Round started Round={Round} Side={Side}", State.Round, RiftSides.Name(side));

    if (MatchConfig.IsRandom)
    {
      RandomModeService.AnnounceBans(mode);
      MapRefreshService.AddRound(RandomModeService.FighterCount, mode);
    }
    else if (MatchConfig.IsMirror)
    {
      MapRefreshService.AddRound(MirrorModeService.FighterCount, mode);
    }
  }

  private static void CloseBetting(int round, ExecutionMode mode)
  {
    _bettingClose = null;

    if (State.Phase == MatchPhase.InRound && State.Round == round)
      BettingService.Close(mode);
  }

  public static (string Title, string Description) ModeBanner(HeroMode heroMode) =>
    heroMode switch
    {
      HeroMode.Mirror => ("Mirror mode", "Everyone has the same hero and build"),
      _ => ("Random mode", "Random hero and build every round")
    };

  private static void AnnounceBuilds(int round, ExecutionMode mode)
  {
    _buildBanner = null;

    if (State.Phase != MatchPhase.Intermission || State.Round + 1 != round)
      return;

    AnnounceHeroBuilds(mode);
  }

  private static void AnnounceHeroBuilds(ExecutionMode mode)
  {
    if (MatchConfig.IsMirror)
      MirrorModeService.AnnounceBuilds(mode);
    else
      RandomModeService.AnnounceBuilds(mode);
  }

  private static void AnnounceFinalCountdown(ExecutionMode mode)
  {
    var title = $"Round {State.Round + 1}";

    HudService.AnnounceAll(title, State.FormatScore(), mode);
  }

  private static void CancelCountdown()
  {
    _countdown?.Cancel();
    _finalCountdown?.Cancel();
    _buildBanner?.Cancel();
    _bettingClose?.Cancel();
    _countdown = null;
    _finalCountdown = null;
    _buildBanner = null;
    _bettingClose = null;
  }
}
