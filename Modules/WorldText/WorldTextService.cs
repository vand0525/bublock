using System.Numerics;
using Bublock.Shared;
using DeadworksManaged.Api;

namespace Bublock.Modules.WorldText;

public static class WorldTextService
{
  public const string DesignerName = "point_worldtext";

  private static readonly Logger Log = BublockLog.For("WorldText");

  private static readonly Dictionary<string, Entry> Boards =
    new(StringComparer.OrdinalIgnoreCase);

  private sealed record Entry(CPointWorldText Board, WorldTextSpec Spec);

  public readonly record struct BoardInfo(string Id, Vector3 Position, string Text, Vector3 Angle);

  public static bool Create(string id, WorldTextSpec spec, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);

    RemoveTracked(id, log);

    var board = CPointWorldText.Create(
      message: spec.Text,
      position: spec.Position,
      fontSize: spec.FontSize,
      worldUnitsPerPx: spec.WorldUnitsPerPx,
      r: spec.Color.R,
      g: spec.Color.G,
      b: spec.Color.B,
      a: spec.Color.A,
      reorientMode: spec.ReorientMode
    );

    if (board == null)
    {
      log.Warn("Board create failed Id={Id} Position={Position}", id, spec.Position);
      return false;
    }

    // Engine 6712 / Deadworks v0.5.0: Create still writes color + fullbright via
    // keyvalues, but boards were reading solid black in game — stamp again after
    // spawn so lighting cannot leave the text dark.
    board.Fullbright = true;
    board.SetColor(spec.Color.R, spec.Color.G, spec.Color.B, spec.Color.A);

    board.Teleport(
      position: spec.Position,
      angles: spec.Angle,
      velocity: Vector3.Zero
    );

    Boards[id] = new Entry(board, spec);

    log.Debug(
      "Board created Id={Id} Position={Position} Angle={Angle} Scale={Scale}",
      id, spec.Position, spec.Angle, spec.WorldUnitsPerPx);

    return true;
  }

  public static bool Update(string id, string text, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);

    if (!TryGetLive(id, out var entry))
    {
      log.Info("Board update skipped, no such board Id={Id}", id);
      return false;
    }

    entry.Board.SetMessage(text);
    Boards[id] = entry with { Spec = entry.Spec with { Text = text } };

    log.Debug("Board updated Id={Id}", id);
    return true;
  }

  public static bool Remove(string id, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);

    if (!RemoveTracked(id, log))
    {
      log.Info("Board remove skipped, no such board Id={Id}", id);
      return false;
    }

    return true;
  }

  public static int ClearAll(ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var removed = 0;

    foreach (var entity in Entities.ByDesignerName(DesignerName))
    {
      entity.Remove();
      removed++;
    }

    Boards.Clear();

    log.Debug("All boards cleared Removed={Removed}", removed);
    return removed;
  }

  public static IReadOnlyList<BoardInfo> List()
  {
    PruneDead();

    return Boards
      .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
      .Select(pair => new BoardInfo(pair.Key, pair.Value.Spec.Position, pair.Value.Spec.Text, pair.Value.Spec.Angle))
      .ToList();
  }

  private static bool TryGetLive(string id, out Entry entry)
  {
    if (!Boards.TryGetValue(id, out entry!))
      return false;

    if (entry.Board.IsValid)
      return true;

    Boards.Remove(id);
    return false;
  }

  private static bool RemoveTracked(string id, Logger log)
  {
    if (!TryGetLive(id, out var entry))
      return false;

    entry.Board.Remove();
    Boards.Remove(id);

    log.Debug("Board removed Id={Id}", id);
    return true;
  }

  private static void PruneDead()
  {
    foreach (var id in Boards.Where(pair => !pair.Value.Board.IsValid).Select(pair => pair.Key).ToList())
      Boards.Remove(id);
  }
}
