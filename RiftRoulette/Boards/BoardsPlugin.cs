using Bublock.Modules.WorldText;
using Bublock.Shared;
using DeadworksManaged.Api;

namespace RiftRoulette.Boards;

public class BoardsPlugin : DeadworksPluginBase
{
  private static readonly Logger BoardsLog = BublockLog.For("Boards");

  public override string Name => "Rift Roulette Boards";

  public override void OnLoad(bool isReload)
  {
    if (isReload)
      Timer.NextTick(() => BoardService.Redraw());
  }

  public override void OnStartupServer()
  {
    Timer.NextTick(() => BoardService.Redraw());
  }

  [Command("board_redraw", Description = "Redraw every board at the watch spot")]
  public void CmdBoardRedraw(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, BoardsLog, "board_redraw");

    BoardService.Redraw(ExecutionMode.Debug);
    AdminCommand.Reply(caller, "[Boards] Boards redrawn");
  }

  [Command("board_note", Description = "Set the note under the welcome board: board_note <text> (no text clears it)")]
  public void CmdBoardNote(CCitadelPlayerController? caller, params string[] text)
  {
    AdminCommand.Authorize(caller, BoardsLog, "board_note");

    WelcomeNoteStore.Set(WorldTextFormat.FromArgs(text), ExecutionMode.Debug);
    BoardService.Redraw(ExecutionMode.Debug);

    var note = WelcomeNoteStore.Text;
    AdminCommand.Reply(caller, note.Length > 0 ? $"[Boards] Note set: {WorldTextFormat.Preview(note)}" : "[Boards] Note cleared");
  }
}
