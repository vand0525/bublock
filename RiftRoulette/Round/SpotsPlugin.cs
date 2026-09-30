using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Rift;

namespace RiftRoulette.Round;

public class SpotsPlugin : DeadworksPluginBase
{
  private static readonly Logger SpotsLog = BublockLog.For("Spots");

  public override string Name => "Rift Roulette Spots";

  [Command("spots_list", Description = "List every slot's watch and rift start spots: spots_list [green|yellow|center]")]
  public void CmdSpotsList(CCitadelPlayerController? caller, string side = "")
  {
    AdminCommand.Authorize(caller, SpotsLog, "spots_list");

    foreach (var riftSide in Sides(side))
    foreach (var line in SpotCheck.Describe(riftSide))
      AdminCommand.Reply(caller, $"[Spots] {line}");
  }

  [Command("spots_walk", Description = "Teleport yourself through each slot spot and log where you land: spots_walk <watch|sapphire|amber> [green|yellow|center]")]
  public void CmdSpotsWalk(CCitadelPlayerController? caller, string group, string side = "")
  {
    AdminCommand.Authorize(caller, SpotsLog, "spots_walk");

    if (caller == null)
      throw new CommandException("Run spots_walk from your own client console; it moves your hero.");

    if (!SpotCheck.TryParseGroup(group, out var spotGroup))
      throw new CommandException($"Unknown group '{group}'. Use watch, sapphire or amber.");

    var riftSide = side.Length == 0 ? WatchSpot.Side : ParseSide(side);

    AdminCommand.Reply(caller, $"[Spots] {SpotCheck.Walk(caller, Timer, spotGroup, riftSide, ExecutionMode.Debug)}");
  }

  private static IEnumerable<RiftSide> Sides(string side) =>
    side.Length == 0 ? RiftSides.All : [ParseSide(side)];

  private static RiftSide ParseSide(string side) =>
    RiftSides.TryParse(side, out var parsed)
      ? parsed
      : throw new CommandException($"Unknown side '{side}'. Use green, yellow, or center.");
}
