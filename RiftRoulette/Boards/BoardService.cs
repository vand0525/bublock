using Bublock.Modules.WorldText;
using Bublock.Shared;
using RiftRoulette.Betting;
using RiftRoulette.GameLoop;
using RiftRoulette.Stats;

namespace RiftRoulette.Boards;

public static class BoardService
{
  public const string AboutHint = "Type /about to learn how to play and bet";

  // Boards drawn on the first tick after a map start did not show (the empty sign stayed invisible after /restart_now).
  public static readonly int[] MapStartRedrawSeconds = [3, 10];

  private static readonly Logger Log = BublockLog.For("Boards");

  public static void RedrawAfterMapStart(int seconds)
  {
    var tracked = WorldTextService.List().Count;
    var found = Redraw();

    Log.Info("Boards redrawn after map start Pass={Pass} TrackedLive={TrackedLive} WorldTextFound={WorldTextFound}", seconds, tracked, found);
  }

  // Returns how many point_worldtext entities were on the map before the redraw.
  public static int Redraw(ExecutionMode mode = ExecutionMode.Clean)
  {
    var found = WorldTextService.ClearAll(mode);

    WorldTextService.Create("board.welcome", BoardLayout.Welcome("RIFT ROULETTE"), mode);
    WorldTextService.Create("board.hint", BoardLayout.Hint(AboutHint), mode);

    var note = WelcomeNoteStore.Text;

    if (note.Length > 0)
      WorldTextService.Create("board.note", BoardLayout.Note(note), mode);

    StatsService.RefreshBoards(mode);
    BettingService.RefreshBoard(mode);
    EmptySign.Refresh(mode, force: true);

    Log.WithMode(mode).Debug("Boards redrawn Mode={Mode}", MatchConfig.HeroMode);
    return found;
  }
}
