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

  // force: BoardService.Redraw has just cleared every board, so the sign is gone whatever Shown says.
  public static void Refresh(ExecutionMode mode = ExecutionMode.Clean, bool force = false)
  {
    var humans = Participants.Humans().Count;
    var show = humans == 0;

    if (show == Shown && !force)
      return;

    var log = Log.WithMode(mode);

    if (show)
    {
      WorldTextService.Create(TitleId, EmptySignLayout.Title(), mode);
      WorldTextService.Create(JoinId, EmptySignLayout.Join(), mode);
    }
    else if (Shown && !force)
    {
      WorldTextService.Remove(TitleId, mode);
      WorldTextService.Remove(JoinId, mode);
    }

    if (show != Shown)
      log.Info(show ? "Empty sign shown Humans={Humans}" : "Empty sign removed Humans={Humans}", humans);
    else
      log.Debug("Empty sign redrawn Shown={Shown} Humans={Humans}", show, humans);

    Shown = show;
  }
}
