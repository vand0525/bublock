using Bublock.Modules.Movement;
using Bublock.Modules.Restraint;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.GameLoop;
using RiftRoulette.Lobby;
using RiftRoulette.Rift;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.Round;

public static class RoundFlow
{
  private static readonly Logger Log = BublockLog.For("Round");

  public static string RunRound(ITimer timer, ExecutionMode mode = ExecutionMode.Clean) =>
    RiftService.RunRift(timer, Steps(mode, timer), mode);

  public static string CancelRound(ITimer timer, ExecutionMode mode = ExecutionMode.Clean) =>
    RiftService.CancelRift(Steps(mode), timer, mode);

  public static void MoveTeamsToRift(RiftSide side, ExecutionMode mode = ExecutionMode.Clean)
  {
    var (sapphire, amber) = RoundLocations.StartsFor(side);

    var sapphirePlayers = PlayersOnTeam(RiftRouletteTeams.Sapphire).ToList();
    var amberPlayers = PlayersOnTeam(RiftRouletteTeams.Amber).ToList();

    foreach (var player in sapphirePlayers.Concat(amberPlayers))
      RestraintService.Release(player, mode);

    var movedSapphire = TeleportToFightSpots(sapphirePlayers, sapphire, mode);
    var movedAmber = TeleportToFightSpots(amberPlayers, amber, mode);
    var healed = sapphirePlayers.Concat(amberPlayers).Count(player => HealToFull(player, mode));

    Log.WithMode(mode).Debug(
      "Teams moved to rift starts Side={Side} Sapphire={Sapphire} Amber={Amber} Healed={Healed}",
      RiftSides.Name(side),
      movedSapphire,
      movedAmber,
      healed);
  }

  private static bool HealToFull(CCitadelPlayerController player, ExecutionMode mode)
  {
    var pawn = player.GetHeroPawn();

    if (pawn == null || !pawn.IsAlive)
      return false;

    var before = pawn.Health;
    pawn.Heal(pawn.GetMaxHealth());

    Log.WithMode(mode).Debug(player.ToPlayerRef(), "Healed to full Health={Health} MaxHealth={MaxHealth}", before, pawn.GetMaxHealth());
    return true;
  }

  public static int SendPlayersUp(ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var side = RiftService.NextSide;
    var returned = 0;

    foreach (var player in Participants.Humans())
    {
      var pawn = player.GetHeroPawn();

      if (pawn == null)
        continue;

      if (!pawn.IsAlive)
      {
        RestraintService.Restrain(player, mode);
        log.Debug(player.ToPlayerRef(), "Return skipped, still dead - waiting for respawn");
        continue;
      }

      if (WatchSpot.SendUp(player, mode, side))
        returned++;
    }

    WatchSpot.RefreshBoards(side, mode);

    log.Debug("Players returned to the watch spot Side={Side} Returned={Returned}", RiftSides.Name(side), returned);
    return returned;
  }

  public static RiftRoundSteps Steps(ExecutionMode mode, ITimer? probeTimer = null) =>
    new(
      side =>
      {
        MoveTeamsToRift(side, mode);

        if (probeTimer != null)
          MatchProbe.SnapshotLater(probeTimer, "moved-in");
      },
      () =>
      {
        var returned = SendPlayersUp(mode);

        if (probeTimer != null)
          MatchProbe.SnapshotLater(probeTimer, "sent-up");

        return returned;
      },
      result => MatchService.OnRoundEnded(result, mode));

  private static int TeleportToFightSpots(
    IEnumerable<CCitadelPlayerController> players,
    MovementLocation anchor,
    ExecutionMode mode) =>
    players.Count(player => MovementService.TeleportTo(player, SlotSpots.Fight(anchor, player.Slot), mode));

  private static IEnumerable<CCitadelPlayerController> PlayersOnTeam(int team) =>
    Participants.Humans().Where(player =>
      player.TeamNum == team && RoundHeroes.Has(player.PlayerSteamId));
}
