using System.Text.RegularExpressions;

namespace Bublock.Modules.Movement;

public sealed class LocationRegistry
{
  private static readonly Regex ValidName = new("^[A-Za-z0-9_.-]+$", RegexOptions.Compiled);

  private readonly Dictionary<string, Entry> _locations = new(StringComparer.OrdinalIgnoreCase);

  private sealed record Entry(MovementLocation Location, bool Saved);

  public readonly record struct Listing(MovementLocation Location, bool Saved);

  public static bool IsValidName(string name) => ValidName.IsMatch(name);

  public void Register(MovementLocation location, bool saved = false)
  {
    if (!IsValidName(location.Name))
      throw new ArgumentException($"Invalid location name '{location.Name}'.", nameof(location));

    _locations[location.Name] = new Entry(location, saved);
  }

  public bool Unregister(string name)
  {
    if (!_locations.TryGetValue(name, out var entry) || !entry.Saved)
      return false;

    return _locations.Remove(name);
  }

  public bool TryGet(string name, out MovementLocation location)
  {
    if (_locations.TryGetValue(name, out var entry))
    {
      location = entry.Location;
      return true;
    }

    location = null!;
    return false;
  }

  public bool IsSaved(string name) =>
    _locations.TryGetValue(name, out var entry) && entry.Saved;

  public IReadOnlyList<Listing> List() =>
    _locations.Values
      .OrderBy(entry => entry.Location.Name, StringComparer.OrdinalIgnoreCase)
      .Select(entry => new Listing(entry.Location, entry.Saved))
      .ToList();
}
