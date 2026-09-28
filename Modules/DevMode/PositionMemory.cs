using System.Numerics;
using Bublock.Modules.Movement;
using Bublock.Shared;
using DeadworksManaged.Api;

namespace Bublock.Modules.DevMode;

// Where players stood in dev, so stopping a live session puts them back there.
public sealed class PositionMemory
{
  private readonly Dictionary<ulong, (Vector3 Position, Vector3 Angles)> _saved = [];
  private readonly Logger _log = BublockLog.For("DevMode");

  public int Count => _saved.Count;

  public int Save(IEnumerable<CCitadelPlayerController> players)
  {
    _saved.Clear();

    foreach (var player in players)
    {
      if (MovementService.Where(player) is { } where)
        _saved[player.PlayerSteamId] = (where.Position, where.EyeAngles);
    }

    _log.Info("Dev positions saved Count={Count}", _saved.Count);
    return _saved.Count;
  }

  public int Restore(IEnumerable<CCitadelPlayerController> players, ExecutionMode mode = ExecutionMode.Clean)
  {
    var restored = 0;

    foreach (var player in players)
    {
      if (!_saved.TryGetValue(player.PlayerSteamId, out var spot))
        continue;

      if (MovementService.TeleportTo(player, new MovementLocation("dev", spot.Position, spot.Angles), mode))
        restored++;
    }

    _log.WithMode(mode).Info("Dev positions restored Count={Count} Saved={Saved}", restored, _saved.Count);
    return restored;
  }

  public void Clear() => _saved.Clear();
}
