using System.Numerics;
using System.Text.Json;
using Bublock.Modules.Movement;
using Bublock.Shared;

namespace RiftRoulette.Round;

public sealed record SpotOffsets(IReadOnlyList<Vector3> Watch, IReadOnlyList<Vector3> Fight);

public static class SlotSpots
{
  public const string ResourceName = "RiftRoulette.Round.spots.json";

  private static readonly Lazy<SpotOffsets> Embedded = new(LoadEmbedded);

  private static readonly HashSet<int> WarnedSlots = [];

  public static SpotOffsets Offsets => Embedded.Value;

  public static MovementLocation Watch(MovementLocation anchor, int slot) =>
    At(anchor, Offsets.Watch, slot);

  public static MovementLocation Fight(MovementLocation anchor, int slot) =>
    At(anchor, Offsets.Fight, slot);

  public static MovementLocation At(MovementLocation anchor, IReadOnlyList<Vector3> offsets, int slot)
  {
    var index = Index(slot, offsets.Count);

    if (index != slot && WarnedSlots.Add(slot))
      BublockLog.For("Round").Warn("Slot outside the spot table Slot={Slot} Used={Used}", slot, index);

    return anchor.Offset(offsets[index], $"{anchor.Name}#{slot}");
  }

  public static int Index(int slot, int count) =>
    ((slot % count) + count) % count;

  public static SpotOffsets Parse(string json)
  {
    var root = JsonSerializer.Deserialize<Dictionary<string, float[][]>>(json)
      ?? throw new InvalidOperationException("spots.json is empty.");

    return new SpotOffsets(Read(root, "watch"), Read(root, "fight"));
  }

  private static IReadOnlyList<Vector3> Read(Dictionary<string, float[][]> root, string key)
  {
    if (!root.TryGetValue(key, out var rows) || rows.Length == 0)
      throw new InvalidOperationException($"spots.json has no '{key}' offsets.");

    return rows
      .Select(row => row.Length == 3
        ? new Vector3(row[0], row[1], row[2])
        : throw new InvalidOperationException($"spots.json '{key}' entry needs [forward, right, up]."))
      .ToList();
  }

  private static SpotOffsets LoadEmbedded()
  {
    using var stream = typeof(SlotSpots).Assembly.GetManifestResourceStream(ResourceName)
      ?? throw new InvalidOperationException($"Embedded resource {ResourceName} not found.");
    using var reader = new StreamReader(stream);

    return Parse(reader.ReadToEnd());
  }
}
