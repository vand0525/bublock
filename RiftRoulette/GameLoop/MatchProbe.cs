using Bublock.Modules.Movement;
using Bublock.Modules.Restraint;
using Bublock.Modules.WorldText;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Lobby;
using RiftRoulette.Rift;
using RiftRoulette.Round;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.GameLoop;

public static class MatchProbe
{
  public const double DelaySeconds = 1.0;

  public static readonly IReadOnlyList<string> CountedDesignerNames =
  [
    "npc_trooper_boss",
    "npc_boss_tier2",
    "npc_barrack_boss",
    "citadel_shop_prop_dynamic"
  ];

  private static readonly Logger Log = BublockLog.For("Probe");

  public static void SnapshotLater(ITimer timer, string reason) =>
    timer.Once(DelaySeconds.Seconds(), () => Snapshot(reason));

  public static void Snapshot(string reason)
  {
    var map = string.Join(" ", CountedDesignerNames.Select(name => $"{name}={Entities.ByDesignerName(name).Count()}"));

    Log.Info(
      "Probe {Reason} MatchRound={MatchRound} Mode={Mode} Phase={Phase} WatchSide={WatchSide} BoardSide={BoardSide} NextSide={NextSide} Rift={Rift} RiftSide={RiftSide} BuyAnywhereConVar={BuyAnywhereConVar} Restrained={Restrained} Rescues={Rescues} Map={Map}",
      reason,
      MatchService.State.Round,
      MatchConfig.HeroMode,
      MatchService.State.Phase,
      RiftSides.Name(WatchSpot.Side),
      RiftSides.Name(WatchSpot.BoardSide),
      RiftSides.Name(RiftService.NextSide),
      RiftService.Phase,
      RiftService.CurrentSide is { } current ? RiftSides.Name(current) : "-",
      ShopAccess.ConVarValue?.ToString() ?? "missing",
      RestraintService.Count,
      WatchGuard.Rescues,
      map);

    foreach (var player in Participants.Humans())
      SnapshotPlayer(player, reason);

    foreach (var board in WorldTextService.List())
      Log.Info("Probe board {Reason} Id={Id} Position={Position} Angle={Angle}", reason, board.Id, board.Position, board.Angle);
  }

  private static void SnapshotPlayer(CCitadelPlayerController player, string reason)
  {
    var pawn = player.GetHeroPawn();
    var where = MovementService.Where(player);
    var states = pawn?.ModifierProp is { } props
      ? string.Join(",", RestraintService.States.Where(props.HasModifierState))
      : "no pawn";
    var half = where is { } spot
      ? spot.Position.Y > 0f ? RiftRouletteTeams.Name(RiftRouletteTeams.Sapphire) : RiftRouletteTeams.Name(RiftRouletteTeams.Amber)
      : "-";

    Log.Info(
      player.ToPlayerRef(),
      "Probe player {Reason} Team={Team} Hero={Hero} Alive={Alive} Position={Position} EyeAngles={EyeAngles} Half={Half} Restrained={Restrained} States={States}",
      reason,
      RiftRouletteTeams.Name(player.TeamNum),
      pawn?.HeroID.ToString() ?? "-",
      pawn?.IsAlive ?? false,
      where?.Position.ToString() ?? "-",
      where?.EyeAngles.ToString() ?? "-",
      half,
      RestraintService.IsRestrained(player.PlayerSteamId),
      states.Length == 0 ? "none" : states);
  }
}
