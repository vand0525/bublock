using Bublock.Shared;
using DeadworksManaged.Api;

namespace Bublock.Modules.Restraint;

public static class RestraintService
{
  public const string SilenceModifier = "modifier_citadel_silenced";

  public const float ModifierDurationSeconds = 100_000f;

  public const int ModifierCheckTicks = 32;

  public static readonly string[] Modifiers = [SilenceModifier];

  // Disarmed also blocks reloading, so players would start rounds on an empty magazine.
  public static readonly EModifierState[] States =
  [
    EModifierState.Silenced,
    EModifierState.ItemsDisabled,
    EModifierState.ShootingDisabled,
    EModifierState.MeleeDisabled,
    EModifierState.IgnoredByNpcTargeting
  ];

  private static readonly Logger Log = BublockLog.For("Restraint");

  private static readonly HashSet<ulong> Restrained = [];

  private static int _tick;

  public static int Count => Restrained.Count;

  public static bool IsRestrained(ulong steamId) => Restrained.Contains(steamId);

  // Runs on every damage event, so it bails out before any entity lookup when nobody is restrained.
  public static bool IsRestrainedPawn(CBaseEntity? entity) =>
    Restrained.Count > 0 &&
    entity?.As<CCitadelPlayerPawn>()?.Controller is { } player &&
    Restrained.Contains(player.PlayerSteamId);

  public static bool Restrain(CCitadelPlayerController player, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!Restrained.Add(player.PlayerSteamId))
      return false;

    var pawn = player.GetHeroPawn();

    if (pawn != null && pawn.IsAlive)
      Hold(player, pawn, checkModifiers: true);

    Log.WithMode(mode).Debug(player.ToPlayerRef(), "Restrained PawnAlive={PawnAlive}", pawn?.IsAlive ?? false);
    return true;
  }

  public static bool Release(CCitadelPlayerController player, ExecutionMode mode = ExecutionMode.Clean)
  {
    if (!Restrained.Remove(player.PlayerSteamId))
      return false;

    var pawn = player.GetHeroPawn();
    var removed = 0;

    if (pawn != null)
    {
      foreach (var name in Modifiers)
      {
        if (pawn.RemoveModifier(name))
          removed++;
      }

      var states = pawn.ModifierProp;

      if (states != null)
      {
        foreach (var state in States)
          states.SetModifierState(state, false);
      }
    }

    Log.WithMode(mode).Debug(player.ToPlayerRef(), "Released ModifiersRemoved={ModifiersRemoved}", removed);
    return true;
  }

  public static void Forget(ulong steamId) => Restrained.Remove(steamId);

  public static void Sustain()
  {
    if (Restrained.Count == 0)
      return;

    var checkModifiers = ++_tick % ModifierCheckTicks == 0;

    foreach (var player in Players.GetAll())
    {
      if (!Restrained.Contains(player.PlayerSteamId))
        continue;

      var pawn = player.GetHeroPawn();

      if (pawn != null && pawn.IsAlive)
        Hold(player, pawn, checkModifiers);
    }
  }

  public static bool AddModifier(CCitadelPlayerPawn pawn, string name, float seconds)
  {
    using var kv = new KeyValues3();
    kv.SetFloat("duration", seconds);
    return pawn.AddModifier(name, kv) != null;
  }

  public static IReadOnlyList<string> Describe()
  {
    var lines = new List<string> { $"Restrained={Restrained.Count} | Modifiers={string.Join(",", Modifiers)} | States={string.Join(",", States)}" };

    foreach (var player in Players.GetAll().Where(player => Restrained.Contains(player.PlayerSteamId)))
    {
      var pawn = player.GetHeroPawn();
      var active = pawn?.ModifierProp is { } states
        ? string.Join(",", Modifiers.Where(states.HasModifier)) is { Length: > 0 } names ? names : "none"
        : "no pawn";

      lines.Add($"Slot={player.Slot} | {player.PlayerName} | Alive={pawn?.IsAlive ?? false} | ActiveModifiers={active}");
    }

    return lines;
  }

  private static void Hold(CCitadelPlayerController player, CCitadelPlayerPawn pawn, bool checkModifiers)
  {
    var states = pawn.ModifierProp;

    if (states == null)
      return;

    foreach (var state in States)
      states.SetModifierState(state, true);

    if (!checkModifiers)
      return;

    foreach (var name in Modifiers)
    {
      if (!states.HasModifier(name) && !AddModifier(pawn, name, ModifierDurationSeconds))
        Log.Trace(player.ToPlayerRef(), "Restraint modifier refused Modifier={Modifier}", name);
    }
  }
}
