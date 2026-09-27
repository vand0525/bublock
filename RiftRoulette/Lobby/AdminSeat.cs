using Bublock.Modules.Restraint;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Draft;
using RiftRoulette.Duel;
using RiftRoulette.GameLoop;
using RiftRoulette.RandomMode;
using RiftRoulette.Rift;
using RiftRoulette.Round;
using RiftRoulette.Stats;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.Lobby;

public static class AdminSeat
{
  public const int SpectatorTeam = 1;
  public const int HeroCheckSeconds = 2;

  private static readonly Logger Log = BublockLog.For("Lobby");

  private static readonly HashSet<ulong> Seated = [];

  public static int SeatedCount => Seated.Count;

  public static bool IsSeated(ulong steamId) => Seated.Contains(steamId);

  public static bool AllowConnect(ulong steamId, string name)
  {
    var playing = Participants.Humans().Count;
    var isAdmin = AdminAuth.IsAuthorized(steamId);
    var allowed = AdminSeatRule.CanConnect(isAdmin, playing);

    if (!allowed)
      Log.Warn("Connection refused, player slots full Name={Name} SteamId={SteamId} Playing={Playing}", name, steamId, playing);
    else if (isAdmin && playing >= AdminSeatRule.PlayerCap)
      Log.Info("Admin connecting into the reserved seat Name={Name} SteamId={SteamId} Playing={Playing}", name, steamId, playing);

    return allowed;
  }

  public static bool SeatOnJoin(CCitadelPlayerController player) =>
    AdminSeatRule.SeatOnJoin(AdminAuth.IsAuthorized(player.PlayerSteamId));

  public static string Sit(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var steamId = player.PlayerSteamId;

    if (!Seated.Add(steamId))
      return $"{player.PlayerName} is already in the admin seat.";

    if (DraftState.Release(steamId, out var hero))
    {
      log.Info(player.ToPlayerRef(), "Seated player released pick Hero={Hero}", hero);
      DraftService.RedrawBoards(mode);
    }

    RandomModeService.Forget(steamId);
    DuelService.Forget(steamId);
    RestraintService.Release(player, mode);
    WatchGuard.Forget(steamId);

    log.Info(player.ToPlayerRef(), "Admin seat taken, spectating next tick Phase={Phase}", RiftService.Phase);
    BublockLog.Master.Info("Admin seated {Player}", player.PlayerName);

    timer.NextTick(() => BecomeObserver(steamId, mode));

    StatsService.RefreshBoards(mode);
    AutoStartService.Check(timer, mode);

    return $"{player.PlayerName} is in the admin seat (spectator). Use dw_seat_play to play.";
  }

  private static void BecomeObserver(ulong steamId, ExecutionMode mode)
  {
    var log = Log.WithMode(mode);
    var player = Players.GetAll().FirstOrDefault(candidate => candidate.PlayerSteamId == steamId);

    if (player == null || !Seated.Contains(steamId))
    {
      log.Info("Spectate skipped, player gone or no longer seated SteamId={SteamId}", steamId);
      return;
    }

    player.ChangeTeam(SpectatorTeam, false);
    player.MakeObserver();

    log.Info(
      player.ToPlayerRef(),
      "Admin spectating TeamNum={TeamNum} HeroPawn={HeroPawn} Observer={Observer}",
      player.TeamNum,
      player.GetHeroPawn() != null,
      player.Pawn?.DesignerName ?? "none");
  }

  public static string Stand(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;

    if (!Seated.Contains(steamId))
      return $"{player.PlayerName} is not in the admin seat.";

    var playing = Participants.Humans().Count;

    if (!AdminSeatRule.CanStand(playing))
      return $"All {AdminSeatRule.PlayerCap} player slots are taken; staying in the admin seat.";

    Seated.Remove(steamId);

    var team = LobbyService.AdmitPlayer(player, timer, mode);

    Log.WithMode(mode).Info(player.ToPlayerRef(), "Admin left the seat Team={Team}", RiftRouletteTeams.Name(team));
    BublockLog.Master.Info("Admin left the seat {Player}", player.PlayerName);

    timer.Once(HeroCheckSeconds.Seconds(), () => CheckHero(steamId, mode));

    return $"{player.PlayerName} is playing on {RiftRouletteTeams.Name(team)}.";
  }

  private static void CheckHero(ulong steamId, ExecutionMode mode)
  {
    var player = Players.GetAll().FirstOrDefault(candidate => candidate.PlayerSteamId == steamId);

    if (player == null)
      return;

    var hasHero = player.GetHeroPawn() != null;
    var log = Log.WithMode(mode);
    var who = player.ToPlayerRef();
    var pawn = player.Pawn?.DesignerName ?? "none";

    if (hasHero)
      log.Info(who, "Admin hero after leaving the seat TeamNum={TeamNum} Pawn={Pawn}", player.TeamNum, pawn);
    else
      log.Warn(who, "Admin has no hero after leaving the seat TeamNum={TeamNum} Pawn={Pawn}", player.TeamNum, pawn);
  }

  public static void Forget(ulong steamId) => Seated.Remove(steamId);

  public static IReadOnlyList<string> Describe()
  {
    var lines = new List<string>
    {
      $"Playing={Participants.Humans().Count}/{AdminSeatRule.PlayerCap} | Seated={Seated.Count} | SpectatorTeam={SpectatorTeam} | " +
      $"maxplayers={ConVar.Find("maxplayers")?.GetInt()} | visible={ConVar.Find("sv_visiblemaxplayers")?.GetInt()}"
    };

    foreach (var player in Players.GetAll().Where(player => Seated.Contains(player.PlayerSteamId)))
      lines.Add($"Seated: Slot={player.Slot} | {player.PlayerName} | TeamNum={player.TeamNum} | Pawn={(player.GetHeroPawn() != null ? "yes" : "none")}");

    return lines;
  }
}
