using System.Numerics;
using Bublock.Modules.Movement;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Rift;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.Round;

public enum SpotGroup
{
  Watch,
  Sapphire,
  Amber
}

public static class SpotCheck
{
  public const double StepSeconds = 1.5;
  public const float Tolerance = 32f;

  private static readonly Logger Log = BublockLog.For("Spots");

  private static IHandle? _walk;

  public static bool TryParseGroup(string? name, out SpotGroup group)
  {
    switch (name?.Trim().ToLowerInvariant())
    {
      case "watch":
        group = SpotGroup.Watch;
        return true;
      case "sapphire":
        group = SpotGroup.Sapphire;
        return true;
      case "amber":
        group = SpotGroup.Amber;
        return true;
      default:
        group = SpotGroup.Watch;
        return false;
    }
  }

  public static IReadOnlyList<MovementLocation> Spots(SpotGroup group, RiftSide side)
  {
    var (sapphire, amber) = RoundLocations.StartsFor(side);

    return Enumerable.Range(0, SlotSpots.Offsets.Watch.Count)
      .Select(slot => group switch
      {
        SpotGroup.Sapphire => SlotSpots.Fight(sapphire, slot),
        SpotGroup.Amber => SlotSpots.Fight(amber, slot),
        _ => SlotSpots.Watch(RoundLocations.WatchFor(side), slot)
      })
      .ToList();
  }

  public static IReadOnlyList<string> Describe(RiftSide side)
  {
    var watch = Spots(SpotGroup.Watch, side);
    var sapphire = Spots(SpotGroup.Sapphire, side);
    var amber = Spots(SpotGroup.Amber, side);

    return Enumerable.Range(0, watch.Count)
      .Select(slot =>
        $"{RiftSides.Name(side)} Slot={slot} | watch={Format(watch[slot].Position)} | " +
        $"sapphire={Format(sapphire[slot].Position)} | amber={Format(amber[slot].Position)}")
      .ToList();
  }

  public static string Walk(
    CCitadelPlayerController player,
    ITimer timer,
    SpotGroup group,
    RiftSide side,
    ExecutionMode mode = ExecutionMode.Debug)
  {
    if (RiftService.IsRunning)
      return "A round is running; walk the spots between rounds.";

    if (_walk != null)
      return "A spot walk is already running.";

    var spots = Spots(group, side);
    var warnings = 0;

    Log.WithMode(mode).Info(player.ToPlayerRef(), "Spot walk started Group={Group} Side={Side} Spots={Spots}", group, RiftSides.Name(side), spots.Count);

    _walk = timer.Sequence(step =>
    {
      var pawn = player.GetHeroPawn();

      if (pawn == null || !pawn.IsAlive)
        return Finish(step, player, group, side, warnings, "stopped, no live pawn", mode);

      if (step.Run > 0 && !CheckLanding(player, pawn.Position, spots[step.Run - 1], mode))
        warnings++;

      if (step.Run >= spots.Count)
        return Finish(step, player, group, side, warnings, "done", mode);

      WatchGuard.Grace(player.PlayerSteamId);
      MovementService.TeleportTo(player, spots[step.Run], mode);
      return step.Wait(StepSeconds.Seconds());
    });

    return $"Walking {spots.Count} {group} spots on {RiftSides.Name(side)}, one every {StepSeconds} s. Results in spots-*.log.";
  }

  private static bool CheckLanding(CCitadelPlayerController player, Vector3 landed, MovementLocation spot, ExecutionMode mode)
  {
    var log = Log.WithMode(mode);
    var flat = Vector2.Distance(new Vector2(landed.X, landed.Y), new Vector2(spot.Position.X, spot.Position.Y));
    var dz = landed.Z - spot.Position.Z;

    if (flat <= Tolerance && MathF.Abs(dz) <= Tolerance)
    {
      log.Info(player.ToPlayerRef(), "Spot ok Spot={Spot} Landed={Landed}", spot.Name, landed);
      return true;
    }

    log.Warn(
      player.ToPlayerRef(),
      "Spot moved the pawn Spot={Spot} Target={Target} Landed={Landed} Flat={Flat} Dz={Dz}",
      spot.Name,
      spot.Position,
      landed,
      flat,
      dz);
    return false;
  }

  private static Pace Finish(
    IStep step,
    CCitadelPlayerController player,
    SpotGroup group,
    RiftSide side,
    int warnings,
    string result,
    ExecutionMode mode)
  {
    _walk = null;

    if (player.GetHeroPawn() is { IsAlive: true })
      WatchSpot.SendUp(player, mode);

    Log.WithMode(mode).Info(
      player.ToPlayerRef(),
      "Spot walk finished Result={Result} Group={Group} Side={Side} Warnings={Warnings}",
      result,
      group,
      RiftSides.Name(side),
      warnings);

    player.PrintToConsole($"[Spots] Walk {result}: {group} on {RiftSides.Name(side)}, {warnings} spot(s) moved the pawn.");
    return step.Done();
  }

  private static string Format(Vector3 position) =>
    FormattableString.Invariant($"({position.X:0},{position.Y:0},{position.Z:0})");
}
