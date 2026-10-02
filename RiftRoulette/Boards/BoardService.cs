using Bublock.Modules.WorldText;
using Bublock.Shared;
using RiftRoulette.Betting;
using RiftRoulette.GameLoop;
using RiftRoulette.Stats;

namespace RiftRoulette.Boards;

public static class BoardService
{
  public const string AboutHint = "Type /about to learn how to play and bet";

  private static readonly Logger Log = BublockLog.For("Boards");

  public static void Redraw(ExecutionMode mode = ExecutionMode.Clean)
  {
    WorldTextService.ClearAll(mode);

    WorldTextService.Create("board.welcome", BoardLayout.Welcome("RIFT ROULETTE"), mode);
    WorldTextService.Create("board.hint", BoardLayout.Hint(AboutHint), mode);

    var note = WelcomeNoteStore.Text;

    if (note.Length > 0)
      WorldTextService.Create("board.note", BoardLayout.Note(note), mode);

    StatsService.RefreshBoards(mode);
    BettingService.RefreshBoard(mode);

    Log.WithMode(mode).Debug("Boards redrawn Mode={Mode}", MatchConfig.HeroMode);
  }
}
