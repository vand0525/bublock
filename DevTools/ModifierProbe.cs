using Bublock.Shared;
using DeadworksManaged.Api;

namespace DevTools;

public static class ModifierProbe
{
  private static readonly Logger Log = BublockLog.For("Modifiers");

  private static readonly HashSet<string> Seen = new(StringComparer.OrdinalIgnoreCase);

  public static int Count => Seen.Count;

  // Runs on every modifier the game adds, so only the first sighting of a name does any work.
  public static void Observe(AddModifierEvent args)
  {
    var name = args.ModifierVData.Name;

    if (string.IsNullOrEmpty(name) || !Seen.Add(name))
      return;

    Log.Info(
      "Modifier seen for the first time Name={Name} Caster={Caster} Owner={Owner}",
      name,
      Describe(args.Caster),
      Describe(args.ModifierProperty.Owner));
  }

  private static string Describe(CBaseEntity? entity) =>
    entity == null
      ? "-"
      : entity.As<CCitadelPlayerPawn>()?.Controller is { } player
        ? player.PlayerName
        : entity.DesignerName;
}
