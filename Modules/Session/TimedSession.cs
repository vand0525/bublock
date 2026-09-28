using Bublock.Shared;
using DeadworksManaged.Api;
using ITimer = DeadworksManaged.Api.ITimer;

namespace Bublock.Modules.Session;

// One continuous session of fixed-length matches: Waiting -> Playing -> Break -> (Playing again).
// The game type owns one instance, feeds it the player count, and draws its own banners in the callbacks.
public sealed class TimedSession
{
  private readonly Logger _log;
  private readonly Func<int> _players;
  private IHandle? _end;
  private IHandle? _warning;
  private IHandle? _next;
  private DateTime? _endsAt;
  private int _pausedLeft;

  public TimedSession(string name, SessionOptions options, Func<int> players)
  {
    Name = name;
    Options = options;
    _players = players;
    _log = BublockLog.For("Session");
  }

  public string Name { get; }

  public SessionOptions Options { get; private set; }

  public SessionPhase Phase { get; private set; } = SessionPhase.Waiting;

  public int Match { get; private set; }

  public Scoreboard Scores { get; } = new();

  public bool IsPlaying => Phase == SessionPhase.Playing;

  public bool IsPaused => Phase == SessionPhase.Paused;

  // Off: a waiting session never starts by itself (a dev sandbox); Start / Resume still work.
  public bool AutoStart { get; set; } = true;

  public int SecondsLeft =>
    IsPaused ? _pausedLeft
    : _endsAt is { } endsAt ? Math.Max(0, (int)Math.Ceiling((endsAt - DateTime.UtcNow).TotalSeconds)) : 0;

  public Action<ExecutionMode>? Started { get; set; }

  public Action<int, ExecutionMode>? Warned { get; set; }

  public Action<MatchResult, ExecutionMode>? Ended { get; set; }

  public Action<ExecutionMode>? Stopped { get; set; }

  public Action<int, ExecutionMode>? PausedAt { get; set; }

  public Action<int, ExecutionMode>? Resumed { get; set; }

  // Call on join (a little later), on leave (with the count minus the leaver) and after load.
  public SessionAction Check(ITimer timer, ExecutionMode mode = ExecutionMode.Clean, int? players = null)
  {
    var count = players ?? _players();
    var action = SessionRule.Decide(Phase, count, Options.MinPlayers, AutoStart);

    _log.WithMode(mode).Debug("Session check Session={Session} Phase={Phase} Players={Players} AutoStart={AutoStart} Action={Action}", Name, Phase, count, AutoStart, action);

    if (action == SessionAction.Start)
      Start(timer, mode);
    else if (action == SessionAction.Stop)
      Stop(mode);

    return action;
  }

  public void Start(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    CancelTimers();
    Phase = SessionPhase.Playing;
    Match++;
    Scores.Clear();
    _endsAt = DateTime.UtcNow.AddSeconds(Options.MatchSeconds);
    Schedule(timer, Options.MatchSeconds, mode);

    _log.WithMode(mode).Info("Match started Session={Session} Match={Match} Seconds={Seconds}", Name, Match, Options.MatchSeconds);
    BublockLog.Master.Info("Match started Session={Session} Match={Match} Seconds={Seconds}", Name, Match, Options.MatchSeconds);
    Started?.Invoke(mode);
  }

  // Freezes the match clock: no end, no warning, no points (IsPlaying is false) until Resume.
  public bool Pause(ExecutionMode mode = ExecutionMode.Clean)
  {
    if (Phase != SessionPhase.Playing)
      return false;

    _pausedLeft = SecondsLeft;
    CancelTimers();
    Phase = SessionPhase.Paused;
    _endsAt = null;

    _log.WithMode(mode).Info("Match paused Session={Session} Match={Match} SecondsLeft={SecondsLeft}", Name, Match, _pausedLeft);
    PausedAt?.Invoke(_pausedLeft, mode);
    return true;
  }

  public bool Resume(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (Phase != SessionPhase.Paused)
      return false;

    var left = Math.Max(1, _pausedLeft);
    Phase = SessionPhase.Playing;
    _endsAt = DateTime.UtcNow.AddSeconds(left);
    Schedule(timer, left, mode);

    _log.WithMode(mode).Info("Match resumed Session={Session} Match={Match} SecondsLeft={SecondsLeft}", Name, Match, left);
    Resumed?.Invoke(left, mode);
    return true;
  }

  public MatchResult? End(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (Phase != SessionPhase.Playing && Phase != SessionPhase.Paused)
      return null;

    CancelTimers();
    var standings = Scores.Standings();
    var result = new MatchResult(Match, standings, SessionRule.Winners(standings));

    Phase = SessionPhase.Break;
    _endsAt = null;

    _log.WithMode(mode).Info(
      "Match ended Session={Session} Match={Match} Winners={Winners} TopPoints={TopPoints} Players={Players}",
      Name,
      Match,
      result.Winners.Count,
      standings.Count > 0 ? standings[0].Points : 0,
      standings.Count);
    BublockLog.Master.Info("Match ended Session={Session} Match={Match} Winners={Winners}", Name, Match, result.Winners.Count);
    Ended?.Invoke(result, mode);

    _next = timer.Once(Options.BreakSeconds.Seconds(), () =>
    {
      _next = null;
      Phase = SessionPhase.Waiting;
      Check(timer, mode);
    });

    return result;
  }

  public void Stop(ExecutionMode mode = ExecutionMode.Clean)
  {
    if (Phase == SessionPhase.Waiting)
      return;

    CancelTimers();
    Phase = SessionPhase.Waiting;
    _endsAt = null;
    Scores.Clear();

    _log.WithMode(mode).Info("Session stopped, waiting for players Session={Session} Match={Match}", Name, Match);
    Stopped?.Invoke(mode);
  }

  // Applies from the next match on.
  public bool TrySetMatchSeconds(int seconds)
  {
    if (!SessionRule.IsValidMatchSeconds(seconds))
      return false;

    Options = Options with { MatchSeconds = seconds };
    return true;
  }

  public IReadOnlyList<string> Describe(Func<ulong, string> nameOf)
  {
    List<string> lines =
    [
      $"{Name} | Phase={Phase} | Match={Match} | Left={SessionRule.Clock(SecondsLeft)} | Length={SessionRule.Clock(Options.MatchSeconds)} | MinPlayers={Options.MinPlayers} | AutoStart={AutoStart}"
    ];

    foreach (var (steamId, points) in Scores.Standings())
      lines.Add($"{Scores.PlaceOf(steamId)}. {nameOf(steamId)} {points}");

    return lines;
  }

  private void Schedule(ITimer timer, int secondsLeft, ExecutionMode mode)
  {
    if (secondsLeft > Options.WarningSeconds && Options.WarningSeconds > 0)
      _warning = timer.Once((secondsLeft - Options.WarningSeconds).Seconds(), () =>
      {
        _warning = null;
        Warned?.Invoke(Options.WarningSeconds, mode);
      });

    _end = timer.Once(secondsLeft.Seconds(), () =>
    {
      _end = null;
      End(timer, mode);
    });
  }

  private void CancelTimers()
  {
    _end?.Cancel();
    _warning?.Cancel();
    _next?.Cancel();
    _end = null;
    _warning = null;
    _next = null;
  }
}
