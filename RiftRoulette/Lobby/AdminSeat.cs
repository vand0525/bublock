using Bublock.Modules.Movement;
using Bublock.Modules.Restraint;
using Bublock.Modules.Spectate;
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

  public const Heroes RoamHero = Heroes.Atlas;
  public const int RoamTeam = RiftRouletteTeams.Amber;
  public const string RoamModifier = "modifier_invis";
  public const int RoamModifierSeconds = 3600;
  public const int FloorCheckSeconds = 2;

  private static readonly Logger Log = BublockLog.For("Lobby");

  private static readonly HashSet<ulong> Seated = [];

  private static readonly HashSet<ulong> Roaming = [];

  private static readonly Dictionary<ulong, int> CloakGeneration = [];

  public static int SeatedCount => Seated.Count;

  public static bool IsSeated(ulong steamId) => Seated.Contains(steamId);

  public static bool IsRoaming(ulong steamId) => Roaming.Contains(steamId);

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

    if (Roaming.Contains(steamId))
    {
      Spectate(player, timer, mode);
      return $"{player.PlayerName} stopped roaming and is spectating.";
    }

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
    StreamCam.Seated(steamId);

    log.Info(
      player.ToPlayerRef(),
      "Admin spectating TeamNum={TeamNum} HeroPawn={HeroPawn} Observer={Observer}",
      player.TeamNum,
      player.GetHeroPawn() != null,
      player.Pawn?.DesignerName ?? "none");
  }

  // Hero swaps made inside OnClientFullConnect are lost, so every switch runs from a timer.
  public static void SyncSoon(ITimer timer, ExecutionMode mode = ExecutionMode.Clean) =>
    timer.Once(HeroCheckSeconds.Seconds(), () => Sync(timer, mode));

  public static void Sync(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var roam = AdminSeatRule.ShouldRoam(Participants.Humans().Count);

    foreach (var player in Players.GetAll().Where(player => Seated.Contains(player.PlayerSteamId)).ToList())
    {
      if (roam == Roaming.Contains(player.PlayerSteamId))
        continue;

      if (roam)
        Roam(player, timer, mode);
      else
        Spectate(player, timer, mode);
    }
  }

  public static string RoamNow(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;
    var playing = Participants.Humans().Count;

    if (!Seated.Contains(steamId))
      return $"{player.PlayerName} is not in the admin seat. Use dw_seat_spec first.";

    if (!AdminSeatRule.ShouldRoam(playing))
      return $"{playing} playing: roaming is only while nobody plays.";

    if (Roaming.Contains(steamId))
    {
      PlaceAndCloak(steamId, timer, mode);
      return $"{player.PlayerName} moved back in front of the welcome sign.";
    }

    Roam(player, timer, mode);
    return $"{player.PlayerName} is roaming (invisible Abrams in front of the welcome sign in 2 s).";
  }

  private static void Roam(CCitadelPlayerController player, ITimer timer, ExecutionMode mode)
  {
    var steamId = player.PlayerSteamId;
    Roaming.Add(steamId);
    CloakGeneration.Remove(steamId);
    StreamCam.Forget(steamId);
    RestraintService.Release(player, mode);
    WatchGuard.Forget(steamId);

    player.SelectHero(RoamHero);
    player.ChangeTeam(RoamTeam, true);

    Log.WithMode(mode).Info(player.ToPlayerRef(), "Admin roaming, server empty Hero={Hero} Team={Team}", RoamHero, RiftRouletteTeams.Name(RoamTeam));
    BublockLog.Master.Info("Admin roaming {Player}", player.PlayerName);

    timer.Once(HeroCheckSeconds.Seconds(), () => PlaceAndCloak(steamId, timer, mode, retry: true));
  }

  private static void Spectate(CCitadelPlayerController player, ITimer timer, ExecutionMode mode)
  {
    var steamId = player.PlayerSteamId;
    Roaming.Remove(steamId);
    CloakGeneration.Remove(steamId);

    Log.WithMode(mode).Info(player.ToPlayerRef(), "Admin roam ended, spectating next tick Playing={Playing}", Participants.Humans().Count);
    timer.NextTick(() => BecomeObserver(steamId, mode));
  }

  public static void PlaceAndCloak(ulong steamId, ITimer timer, ExecutionMode mode = ExecutionMode.Clean, bool retry = false)
  {
    var player = Find(steamId);

    if (player == null || !Roaming.Contains(steamId))
      return;

    var log = Log.WithMode(mode);
    var pawn = player.GetHeroPawn();

    if (pawn == null || !pawn.IsAlive)
    {
      if (!retry)
      {
        log.Warn(player.ToPlayerRef(), "Roaming admin has no hero, not placed Pawn={Pawn}", player.Pawn?.DesignerName ?? "none");
        return;
      }

      log.Warn(player.ToPlayerRef(), "Roaming admin has no hero yet, selecting again Pawn={Pawn}", player.Pawn?.DesignerName ?? "none");
      player.SelectHero(RoamHero);
      timer.Once(HeroCheckSeconds.Seconds(), () => PlaceAndCloak(steamId, timer, mode));
      return;
    }

    // The respawn from the team change usually placed the admin already; don't pull them back.
    if (retry && CloakGeneration.ContainsKey(steamId))
      return;

    var side = WatchSpot.BoardSide;
    var anchor = WatchSpot.Location(side);
    MovementService.TeleportTo(player, WatchLayout.WelcomeFront(anchor, side), mode);
    Cloak(player, timer, mode);
    timer.Once(FloorCheckSeconds.Seconds(), () => CatchFall(steamId, anchor, mode));
  }

  // The skybox floor is invisible collision, missing from the map mesh: the front-of-sign spot may have none.
  private static void CatchFall(ulong steamId, MovementLocation anchor, ExecutionMode mode)
  {
    var player = Find(steamId);
    var pawn = player?.GetHeroPawn();

    if (player == null || pawn == null || !pawn.IsAlive || !Roaming.Contains(steamId))
      return;

    if (!WatchGuardRule.IsBelow(pawn.Position.Z, anchor.Position.Z))
      return;

    Log.WithMode(mode).Warn(player.ToPlayerRef(), "Roaming admin fell from the welcome spot, back to the watch slot Z={Z}", pawn.Position.Z);
    MovementService.TeleportTo(player, SlotSpots.Watch(anchor, player.Slot), mode);
  }

  // One long modifier, put back when it runs out; a newer cloak cancels the older timer.
  public static void Cloak(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;
    var pawn = player.GetHeroPawn();

    if (!Roaming.Contains(steamId) || pawn == null || !pawn.IsAlive)
      return;

    var added = RestraintService.AddModifier(pawn, RoamModifier, RoamModifierSeconds);
    var generation = CloakGeneration[steamId] = CloakGeneration.GetValueOrDefault(steamId) + 1;

    if (added)
      Log.WithMode(mode).Info(player.ToPlayerRef(), "Roaming admin cloaked Modifier={Modifier} Seconds={Seconds}", RoamModifier, RoamModifierSeconds);
    else
      Log.Warn(player.ToPlayerRef(), "Roaming admin cloak refused Modifier={Modifier}", RoamModifier);

    timer.Once(RoamModifierSeconds.Seconds(), () =>
    {
      if (CloakGeneration.GetValueOrDefault(steamId) == generation && Find(steamId) is { } current)
        Cloak(current, timer, mode);
    });
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
    Roaming.Remove(steamId);
    CloakGeneration.Remove(steamId);
    StreamCam.Forget(steamId);
    player.GetHeroPawn()?.RemoveModifier(RoamModifier);

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

  public static void Forget(ulong steamId)
  {
    Seated.Remove(steamId);
    Roaming.Remove(steamId);
    CloakGeneration.Remove(steamId);
    StreamCam.Forget(steamId);
  }

  // Hot reload wipes Seated and Roaming. Every connected admin goes back into the seat; one on a
  // hero pawn is roaming again (cloak re-applied in place), then Sync settles roam vs spectate.
  public static int Restore(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var restored = 0;

    foreach (var player in Players.GetAll())
    {
      var steamId = player.PlayerSteamId;

      if (!AdminAuth.IsAuthorized(steamId) || !Seated.Add(steamId))
        continue;

      var roaming = !SpectateService.IsObserving(player) && player.GetHeroPawn() != null;

      if (roaming)
      {
        Roaming.Add(steamId);
        timer.NextTick(() =>
        {
          if (Find(steamId) is { } current)
            Cloak(current, timer, mode);
        });
      }

      restored++;
      Log.WithMode(mode).Info(player.ToPlayerRef(), "Admin seat restored after reload TeamNum={TeamNum} Roaming={Roaming}", player.TeamNum, roaming);
    }

    SyncSoon(timer, mode);
    return restored;
  }

  private static CCitadelPlayerController? Find(ulong steamId) =>
    Players.GetAll().FirstOrDefault(player => player.PlayerSteamId == steamId);

  public static IReadOnlyList<string> Describe()
  {
    var lines = new List<string>
    {
      $"Playing={Participants.Humans().Count}/{AdminSeatRule.PlayerCap} | Seated={Seated.Count} | Roaming={Roaming.Count} | SpectatorTeam={SpectatorTeam} | " +
      $"maxplayers={ConVar.Find("maxplayers")?.GetInt()} | visible={ConVar.Find("sv_visiblemaxplayers")?.GetInt()}"
    };

    foreach (var player in Players.GetAll().Where(player => Seated.Contains(player.PlayerSteamId)))
      lines.Add($"Seated: Slot={player.Slot} | {player.PlayerName} | TeamNum={player.TeamNum} | Pawn={(player.GetHeroPawn() != null ? "yes" : "none")} | Roaming={Roaming.Contains(player.PlayerSteamId)}");

    return lines;
  }
}
