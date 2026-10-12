using Bublock.Modules.WorldText;
using Bublock.Shared;
using RiftRoulette.Lobby;

namespace RiftRoulette.Boards;

public static class EmptySign
{
  public const string TitleId = "board.empty.title";
  public const string JoinId = "board.empty.join";

  public const int CheckSeconds = 2;

  private static readonly Logger Log = BublockLog.For("Boards");

  public static bool Shown { get; private set; }

  // Shows the sign even with players on, so an admin can adjust it; cleared by a reload.
  public static bool Preview { get; private set; }

  // Judged from the boards that exist, not from Shown: a map change destroys them while the static stays set.
  // force: BoardService.Redraw has just cleared every board, so draw again even if they look live.
  public static void Refresh(ExecutionMode mode = ExecutionMode.Clean, bool force = false)
  {
    try
    {
      var humans = Participants.Humans().Count;
      var show = humans == 0 || Preview;
      var titleLive = WorldTextService.IsLive(TitleId);
      var joinLive = WorldTextService.IsLive(JoinId);

      if (!force && (show ? titleLive && joinLive : !titleLive && !joinLive))
      {
        Shown = show;
        return;
      }

      var log = Log.WithMode(mode);

      if (show)
        Draw(mode);
      else
      {
        if (titleLive)
          WorldTextService.Remove(TitleId, mode);

        if (joinLive)
          WorldTextService.Remove(JoinId, mode);
      }

      if (show != Shown)
        log.Info(show ? "Empty sign shown Humans={Humans} Preview={Preview}" : "Empty sign removed Humans={Humans}", humans, Preview);
      else if (force)
        log.Debug("Empty sign redrawn Shown={Shown} Humans={Humans}", show, humans);
      else
        log.Info("Empty sign repaired Shown={Shown} Humans={Humans} TitleLive={TitleLive} JoinLive={JoinLive}", show, humans, titleLive, joinLive);

      Shown = show;
    }
    catch (Exception exception)
    {
      Log.Error(exception, "Empty sign refresh failed Force={Force}", force);
    }
  }

  // Validates, saves and redraws the sign if it is up. Returns the problem when a value is out of range.
  public static string? Change(Func<EmptySignSettings, EmptySignSettings> change, ExecutionMode mode = ExecutionMode.Clean)
  {
    var settings = change(EmptySignStore.Current);

    if (settings.Problem() is { } problem)
      return problem;

    EmptySignStore.Set(settings, mode);

    if (Shown)
      Draw(mode);

    Log.WithMode(mode).Info("Empty sign changed Settings={Settings} Shown={Shown}", settings.Describe(), Shown);
    return null;
  }

  public static void SetPreview(bool on, ExecutionMode mode = ExecutionMode.Clean)
  {
    Preview = on;
    Log.WithMode(mode).Info("Empty sign preview set Preview={Preview}", on);
    Refresh(mode);
  }

  private static void Draw(ExecutionMode mode)
  {
    var settings = EmptySignStore.Current;

    WorldTextService.Create(TitleId, EmptySignLayout.Title(settings), mode);
    WorldTextService.Create(JoinId, EmptySignLayout.Join(settings), mode);
  }
}
