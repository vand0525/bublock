using Bublock.Modules.Movement;
using Bublock.Modules.Restraint;
using Bublock.Modules.Spectate;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.GameLoop;
using RiftRoulette.Mirror;
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

  public const Heroes RoamHero = LobbyService.LobbyHero;
  public const int RoamTeam = RiftRouletteTeams.Amber;
  public const string RoamModifier = "modifier_invis";
  public const int RoamModifierSeconds = 3600;
  public const int FloorCheckSeconds = 2;
  public const double UncloakSeconds = 0.5;

  private static readonly Logger Log = BublockLog.For("Lobby");

  private static readonly HashSet<ulong> Seated = [];

  private static readonly HashSet<ulong> Roaming = [];

  private static readonly Dictionary<ulong, int> CloakGeneration = [];

  // The watch spot side the roaming admin was last placed at.
  private static readonly Dictionary<ulong, RiftSide> RoamSide = [];

  // Kept through disconnects and map changes (not Forget / ResetForMap): a rejoin restores the mode.
  private static readonly Dictionary<ulong, AdminMode> LastMode = [];

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

  public static AdminMode JoinMode(CCitadelPlayerController player) =>
    AdminSeatRule.JoinMode(
      AdminAuth.IsAuthorized(player.PlayerSteamId),
      Participants.Humans().Count(other => other.PlayerSteamId != player.PlayerSteamId),
      LastMode.TryGetValue(player.PlayerSteamId, out var last) ? last : null);

  public static void Join(CCitadelPlayerController player, AdminMode joinMode, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    Log.WithMode(mode).Info(player.ToPlayerRef(), "Admin joining in last mode Mode={Mode}", joinMode);
    Forget(player.PlayerSteamId);
    Sit(player, timer, mode);

    if (joinMode == AdminMode.Roam)
      Roam(player, timer, mode);
  }

  public static string Sit(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var steamId = player.PlayerSteamId;
    LastMode[steamId] = AdminMode.Spectate;

    if (Roaming.Contains(steamId))
    {
      Spectate(player, timer, mode);
      return $"{player.PlayerName} stopped roaming and is spectating.";
    }

    if (!Seated.Add(steamId))
      return $"{player.PlayerName} is already in the admin seat.";

    if (RoundHeroes.Remove(steamId, out var hero))
      log.Info(player.ToPlayerRef(), "Seated player dropped round hero Hero={Hero}", hero);

    RandomModeService.Forget(steamId);
    MirrorModeService.Forget(steamId);
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

    if (player == null || !Seated.Contains(steamId) || Roaming.Contains(steamId))
    {
      log.Info("Spectate skipped, player gone, no longer seated or roaming SteamId={SteamId}", steamId);
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

  public static string RoamNow(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;

    if (!Seated.Contains(steamId))
    {
      var pawn = player.GetHeroPawn();

      if (pawn != null && !pawn.IsAlive)
        return $"{player.PlayerName} is dead. Roam after the respawn.";

      Sit(player, timer, mode);
      Roam(player, timer, mode);
      return $"{player.PlayerName} left the team and is roaming (invisible Abrams in front of the welcome sign in 2 s).";
    }

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
    LastMode[steamId] = AdminMode.Roam;
    Roaming.Add(steamId);
    CloakGeneration.Remove(steamId);
    RoamSide.Remove(steamId);
    StreamCam.Forget(steamId);
    RestraintService.Release(player, mode);
    WatchGuard.Forget(steamId);

    player.SelectHero(RoamHero);
    player.ChangeTeam(RoamTeam, true);

    Log.WithMode(mode).Info(player.ToPlayerRef(), "Admin roaming Hero={Hero} Team={Team}", RoamHero, RiftRouletteTeams.Name(RoamTeam));
    BublockLog.Master.Info("Admin roaming {Player}", player.PlayerName);

    timer.Once(HeroCheckSeconds.Seconds(), () => PlaceAndCloak(steamId, timer, mode, retry: true));
  }

  private static void Spectate(CCitadelPlayerController player, ITimer timer, ExecutionMode mode)
  {
    var steamId = player.PlayerSteamId;
    Roaming.Remove(steamId);
    CloakGeneration.Remove(steamId);
    RoamSide.Remove(steamId);

    // Deleting the pawn with the cloak still on leaves the client's red invisibility tint on screen.
    var uncloaked = player.GetHeroPawn()?.RemoveModifier(RoamModifier) ?? false;

    Log.WithMode(mode).Info(
      player.ToPlayerRef(),
      "Admin roam ended, spectating shortly Playing={Playing} Uncloaked={Uncloaked}",
      Participants.Humans().Count,
      uncloaked);
    timer.Once(UncloakSeconds.Seconds(), () => BecomeObserver(steamId, mode));
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

    // The hero spawned back from spectating came in at 1 health (2026-09-28).
    var health = pawn.Health;
    var maxHealth = pawn.GetMaxHealth();

    if (health < maxHealth)
    {
      pawn.Heal(maxHealth);
      log.Info(player.ToPlayerRef(), "Roaming admin healed to full Health={Health} MaxHealth={MaxHealth}", health, maxHealth);
    }

    // The respawn from the team change usually placed the admin already; don't pull them back.
    if (retry && CloakGeneration.ContainsKey(steamId))
      return;

    Place(player, timer, mode);
    Cloak(player, timer, mode);
  }

  // Every StreamCam tick: a roaming admin placed at another watch spot side is moved to the current one.
  // Not placed yet means PlaceAndCloak is still on its way (the hero swap after roaming starts).
  public static void FollowWatchSpot(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var side = WatchSpot.BoardSide;

    foreach (var steamId in Roaming.ToList())
    {
      if (!RoamSide.TryGetValue(steamId, out var placed) || placed == side)
        continue;

      if (Find(steamId) is not { } player || player.GetHeroPawn() is not { IsAlive: true })
        continue;

      Log.WithMode(mode).Info(player.ToPlayerRef(), "Roaming admin follows the watch spot From={From} To={To}", RiftSides.Name(placed), RiftSides.Name(side));
      Place(player, timer, mode);
    }
  }

  private static void Place(CCitadelPlayerController player, ITimer timer, ExecutionMode mode)
  {
    var steamId = player.PlayerSteamId;
    var side = WatchSpot.BoardSide;
    var anchor = WatchSpot.Location(side);

    RoamSide[steamId] = side;
    MovementService.TeleportTo(player, WatchLayout.WelcomeFront(anchor, side), mode);
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

    LastMode[steamId] = AdminMode.Play;
    Seated.Remove(steamId);
    Roaming.Remove(steamId);
    CloakGeneration.Remove(steamId);
    RoamSide.Remove(steamId);
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
    RoamSide.Remove(steamId);
    StreamCam.Forget(steamId);
  }

  // A map change keeps clients but not their pawns; they rejoin through OnClientFullConnect in their last mode.
  public static void ResetForMap()
  {
    foreach (var steamId in Seated)
      StreamCam.Forget(steamId);

    Log.Info("Admin seat reset for the new map Seated={Seated} Roaming={Roaming}", Seated.Count, Roaming.Count);
    Seated.Clear();
    Roaming.Clear();
    CloakGeneration.Clear();
    RoamSide.Clear();
  }

  // Hot reload wipes every static here. The mode is read back from the pawn: observing is spectate,
  // a hero carrying the roam cloak is roam (re-cloaked in place), any other hero is play.
  public static int Restore(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var restored = 0;

    foreach (var player in Players.GetAll())
    {
      var steamId = player.PlayerSteamId;

      if (!AdminAuth.IsAuthorized(steamId))
        continue;

      var pawn = player.GetHeroPawn();
      var adminMode = SpectateService.IsObserving(player) ? AdminMode.Spectate
        : pawn?.ModifierProp?.HasModifier(RoamModifier) == true ? AdminMode.Roam
        : AdminMode.Play;

      LastMode[steamId] = adminMode;
      Log.WithMode(mode).Info(player.ToPlayerRef(), "Admin mode restored after reload Mode={Mode} TeamNum={TeamNum}", adminMode, player.TeamNum);

      if (adminMode == AdminMode.Play)
        continue;

      Seated.Add(steamId);
      restored++;

      if (adminMode == AdminMode.Roam)
      {
        Roaming.Add(steamId);
        RoamSide[steamId] = WatchSpot.BoardSide;
        timer.NextTick(() =>
        {
          if (Find(steamId) is { } current)
            Cloak(current, timer, mode);
        });
      }
    }

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
