using Bublock.Shared;
using DeadworksManaged.Api;

namespace Bublock.Modules.WorldText;

public class WorldTextPlugin : DeadworksPluginBase
{
  private static readonly Logger CommandsLog = BublockLog.For("WorldText");

  public override string Name => "World Text";

  [Command("wt_list", Description = "List text boards created through WorldText")]
  public void CmdList(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, CommandsLog, "wt_list");

    var boards = WorldTextService.List();
    AdminCommand.Reply(caller, $"[WorldText] {boards.Count} board(s)");

    foreach (var board in boards)
    {
      AdminCommand.Reply(
        caller,
        $"[WorldText] {board.Id} | Position={board.Position} | {WorldTextFormat.Preview(board.Text)}");
    }
  }

  [Command("wt_create", Description = "Create a text board in front of you: wt_create <id> <text>")]
  public void CmdCreate(CCitadelPlayerController? caller, string id, params string[] text)
  {
    AdminCommand.Authorize(caller, CommandsLog, "wt_create");

    if (text.Length == 0)
      throw new CommandException("Usage: wt_create <id> <text>");

    var pawn = caller?.GetHeroPawn()
      ?? throw new CommandException("Run wt_create in game while you have a hero.");

    var yaw = pawn.EyeAngles.Y;
    var spec = new WorldTextSpec(
      Text: WorldTextFormat.FromArgs(text),
      Position: WorldTextPlacement.InFrontOf(pawn.EyePosition, yaw),
      Angle: WorldTextPlacement.FacingViewer(yaw),
      Color: WorldTextColor.White,
      WorldUnitsPerPx: 0.8f);

    if (!WorldTextService.Create(id, spec, ExecutionMode.Debug))
      throw new CommandException($"Could not create board '{id}'.");

    AdminCommand.Reply(caller, $"[WorldText] Created {id} at {spec.Position}");
  }

  [Command("wt_update", Description = "Change a board's text: wt_update <id> <text>")]
  public void CmdUpdate(CCitadelPlayerController? caller, string id, params string[] text)
  {
    AdminCommand.Authorize(caller, CommandsLog, "wt_update");

    if (text.Length == 0)
      throw new CommandException("Usage: wt_update <id> <text>");

    if (!WorldTextService.Update(id, WorldTextFormat.FromArgs(text), ExecutionMode.Debug))
      throw new CommandException($"No board named '{id}'. See wt_list.");

    AdminCommand.Reply(caller, $"[WorldText] Updated {id}");
  }

  [Command("wt_remove", Description = "Remove one text board: wt_remove <id>")]
  public void CmdRemove(CCitadelPlayerController? caller, string id)
  {
    AdminCommand.Authorize(caller, CommandsLog, "wt_remove");

    if (!WorldTextService.Remove(id, ExecutionMode.Debug))
      throw new CommandException($"No board named '{id}'. See wt_list.");

    AdminCommand.Reply(caller, $"[WorldText] Removed {id}");
  }

  [Command("wt_clear", Description = "Remove every text board on the map")]
  public void CmdClear(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, CommandsLog, "wt_clear");

    var removed = WorldTextService.ClearAll(ExecutionMode.Debug);
    AdminCommand.Reply(caller, $"[WorldText] Removed {removed} board(s)");
  }
}
