using System.Globalization;
using Bublock.Modules.Movement;
using Bublock.Shared;
using DeadworksManaged.Api;

namespace Bublock.Modules.DevMode;

// A point-in-time dump of the server for debugging a live session: every player, then the game type's own lines.
// Written to debug-YYYYMMDD.log (pulled by scripts/pull-logs.sh) and returned for the caller.
public static class DebugSnapshot
{
  private static readonly Logger Log = BublockLog.For("Debug");

  public static IReadOnlyList<string> Take(string reason, IEnumerable<string> gameLines, ExecutionMode mode = ExecutionMode.Debug)
  {
    var players = Players.GetAll().OrderBy(player => player.Slot).ToList();
    List<string> lines =
    [
      $"Snapshot {DateTime.UtcNow.ToString("HH:mm:ss", CultureInfo.InvariantCulture)} UTC | Reason={reason} | Map={Server.MapName} | Players={players.Count(p => !p.IsBot)} | Bots={players.Count(p => p.IsBot)}"
    ];

    foreach (var player in players)
      lines.Add(Describe(player));

    lines.AddRange(gameLines);

    var log = Log.WithMode(mode);
    foreach (var line in lines)
      log.Info("Snapshot {Line}", line);

    BublockLog.Master.Info("Debug snapshot taken Reason={Reason} Lines={Lines}", reason, lines.Count);
    return lines;
  }

  private static string Describe(CCitadelPlayerController player)
  {
    var pawn = player.GetHeroPawn();
    var where = MovementService.Where(player);
    var position = where is { } w ? $"({w.Position.X:0},{w.Position.Y:0},{w.Position.Z:0})" : "-";
    var life = pawn == null ? "no pawn" : pawn.IsAlive ? $"alive {pawn.Health}hp" : "dead";

    return $"{player.Slot}. {player.PlayerName}{(player.IsBot ? " [bot]" : "")} team={player.TeamNum} {life} at {position}";
  }
}
