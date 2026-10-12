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

    Timer.Every(EmptySign.CheckSeconds.Seconds(), () => EmptySign.Refresh());
  }

  public override void OnStartupServer()
  {
    Timer.NextTick(() => BoardService.Redraw());

    foreach (var seconds in BoardService.MapStartRedrawSeconds)
      Timer.Once(seconds.Seconds(), () => BoardService.RedrawAfterMapStart(seconds));
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

  [Command("sign_status", Description = "Empty-server sign: show its saved settings and whether it is up")]
  public void CmdSignStatus(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, BoardsLog, "sign_status");

    AdminCommand.Reply(caller, $"[Boards] Sign Shown={EmptySign.Shown} Preview={EmptySign.Preview}");
    AdminCommand.Reply(caller, $"[Boards] {EmptySignStore.Current.Describe()}");
  }

  [Command("sign_distance", Description = "Empty-server sign: units in front of the camera: sign_distance <units>")]
  public void CmdSignDistance(CCitadelPlayerController? caller, float units) =>
    ChangeSign(caller, "sign_distance", settings => settings with { Distance = units });

  [Command("sign_shift", Description = "Empty-server sign: units to the camera's right (negative: left): sign_shift <units>")]
  public void CmdSignShift(CCitadelPlayerController? caller, float units) =>
    ChangeSign(caller, "sign_shift", settings => settings with { Shift = units });

  [Command("sign_height", Description = "Empty-server sign: units up from the middle of the view (negative: down): sign_height <units>")]
  public void CmdSignHeight(CCitadelPlayerController? caller, float units) =>
    ChangeSign(caller, "sign_height", settings => settings with { Height = units });

  [Command("sign_offset", Description = "Empty-server sign: move one line on its own: sign_offset <title|join> <right> <up> (negative: left / down)")]
  public void CmdSignOffset(CCitadelPlayerController? caller, string line, float right, float up) =>
    ChangeSign(caller, "sign_offset", settings => SignLine(line) switch
    {
      "title" => settings with { TitleRight = right, TitleUp = up },
      "join" => settings with { JoinRight = right, JoinUp = up },
      _ => throw new CommandException("Usage: sign_offset <title|join> <right> <up>")
    });

  [Command("sign_size", Description = "Empty-server sign: text size in world units per pixel: sign_size <title> [join]")]
  public void CmdSignSize(CCitadelPlayerController? caller, float title, float join = -1f) =>
    ChangeSign(caller, "sign_size", settings => settings with { TitleScale = title, JoinScale = join < 0f ? settings.JoinScale : join });

  [Command("sign_gap", Description = "Empty-server sign: units between the middle and each line: sign_gap <units>")]
  public void CmdSignGap(CCitadelPlayerController? caller, float units) =>
    ChangeSign(caller, "sign_gap", settings => settings with { Gap = units });

  [Command("sign_color", Description = "Empty-server sign: line color: sign_color <title|join|both> <red> <green> <blue> (0-255)")]
  public void CmdSignColor(CCitadelPlayerController? caller, string line, int red, int green, int blue)
  {
    var color = new SignColor(red, green, blue);

    ChangeSign(caller, "sign_color", settings => SignLine(line) switch
    {
      "title" => settings with { TitleColor = color },
      "join" => settings with { JoinColor = color },
      _ => settings with { TitleColor = color, JoinColor = color }
    });
  }

  [Command("sign_text", Description = "Empty-server sign: line text: sign_text <title|join> [text] (no text restores the default)")]
  public void CmdSignText(CCitadelPlayerController? caller, string line, params string[] text)
  {
    var joined = string.Join(' ', text).Trim();

    ChangeSign(caller, "sign_text", settings => SignLine(line) switch
    {
      "title" => settings with { TitleText = joined.Length > 0 ? joined : EmptySignSettings.DefaultTitle },
      "join" => settings with { JoinText = joined.Length > 0 ? joined : EmptySignSettings.DefaultJoin },
      _ => throw new CommandException("Usage: sign_text <title|join> [text]")
    });
  }

  [Command("sign_preview", Description = "Empty-server sign: show it even with players on, to adjust it: sign_preview <on|off>")]
  public void CmdSignPreview(CCitadelPlayerController? caller, string state)
  {
    AdminCommand.Authorize(caller, BoardsLog, "sign_preview");

    var on = state.Trim().ToLowerInvariant() switch
    {
      "on" or "1" => true,
      "off" or "0" => false,
      _ => throw new CommandException("Usage: sign_preview <on|off>")
    };

    EmptySign.SetPreview(on, ExecutionMode.Debug);
    AdminCommand.Reply(caller, $"[Boards] Sign preview {(on ? "on" : "off")} | Shown={EmptySign.Shown}");
  }

  [Command("sign_reset", Description = "Empty-server sign: restore every default position, size, color and text")]
  public void CmdSignReset(CCitadelPlayerController? caller) =>
    ChangeSign(caller, "sign_reset", _ => EmptySignSettings.Default);

  private static void ChangeSign(CCitadelPlayerController? caller, string command, Func<EmptySignSettings, EmptySignSettings> change)
  {
    AdminCommand.Authorize(caller, BoardsLog, command);

    if (EmptySign.Change(change, ExecutionMode.Debug) is { } problem)
      throw new CommandException(problem);

    AdminCommand.Reply(caller, $"[Boards] Sign saved{(EmptySign.Shown ? "" : " (not up: players are on; sign_preview on shows it)")}");
    AdminCommand.Reply(caller, $"[Boards] {EmptySignStore.Current.Describe()}");
  }

  private static string SignLine(string line) =>
    line.Trim().ToLowerInvariant() switch
    {
      "title" => "title",
      "join" => "join",
      "both" => "both",
      _ => throw new CommandException("Line must be title, join or both.")
    };
}
