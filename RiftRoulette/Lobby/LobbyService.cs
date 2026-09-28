using Bublock.Modules.Restraint;
using Bublock.Modules.Spectate;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Draft;
using RiftRoulette.Duel;
using RiftRoulette.GameLoop;
using RiftRoulette.RandomMode;
using RiftRoulette.Round;
using RiftRoulette.Stats;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.Lobby;

public static class LobbyService
{
  private static readonly Logger LobbyLog = BublockLog.For("Lobby");

  private static readonly Logger PlayersLog = BublockLog.For("Players");

  public const double OrphanSweepSeconds = 1.0;

  public static void ApplyServerConvars(ExecutionMode mode = ExecutionMode.Clean)
  {
    ServerConVars.TrySet("citadel_team_size", 6, LobbyLog);
    ServerConVars.TrySet("maxplayers", AdminSeatRule.PlayerCap + 1, LobbyLog);
    ServerConVars.TrySet("sv_visiblemaxplayers", AdminSeatRule.PlayerCap, LobbyLog);
    ServerConVars.TrySet("citadel_koth_enabled", 0, LobbyLog);
    Server.ExecuteCommand("citadel_koth_warning_time 1");
    Server.ExecuteCommand("citadel_koth_early_warning_time 1");
    Server.ExecuteCommand("citadel_player_override_spawn_time 1");
    ServerConVars.TrySet("citadel_allow_duplicate_heroes", 1, LobbyLog);
    ServerConVars.TrySet("citadel_hero_demo_unlock_flex_slots", 1, LobbyLog);
    FlexSlots.UnlockAll(mode);
    ShopAccess.Sync(mode);
    PauseGuard.FollowAccess(mode);

    LobbyLog.WithMode(mode).Info(
      "Server convars applied TeamSize={TeamSize} MaxPlayers={MaxPlayers} Visible={Visible}",
      6,
      AdminSeatRule.PlayerCap + 1,
      AdminSeatRule.PlayerCap);
  }

  public static int AdmitPlayer(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;
    var others = Participants.Humans()
      .Where(other => other.PlayerSteamId != steamId)
      .Select(other => other.TeamNum);
    var team = TeamBalance.SmallerTeam(others, Random.Shared);

    FlexSlots.UnlockAll(mode);
    player.SelectHero(Heroes.Skyrunner);
    player.ChangeTeam(team, true);
    WatchSpot.SendUp(player, mode);

    LobbyLog.WithMode(mode).Info(player.ToPlayerRef(), "Player admitted to draft Team={Team}", RiftRouletteTeams.Name(team));

    if (MatchService.State.IsRunning && MatchConfig.IsRandom)
      RandomModeService.AddJoiner(player, team, timer, mode);
    else if (!MatchService.State.IsRunning && MatchConfig.IsDuel)
      DuelService.GrantSetup(player, timer, mode, announce: true);

    StatsService.RefreshBoards(mode);
    AutoStartService.CheckSoon(timer, mode);
    return team;
  }

  public static void RemovePlayer(CCitadelPlayerController player, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;
    AdminSeat.Forget(steamId);
    BanStatueService.Forget(steamId);
    RestraintService.Forget(steamId);
    WatchGuard.Forget(steamId);
    DuelService.Forget(steamId);

    var log = LobbyLog.WithMode(mode);
    log.Info(player.ToPlayerRef(), "Player disconnected");

    if (DraftState.Release(player.PlayerSteamId, out var hero))
    {
      log.Info(player.ToPlayerRef(), "Disconnected player removed Hero={Hero}", hero);
      DraftService.RedrawBoards(mode);
    }

    if (MatchService.State.IsRunning && MatchConfig.IsRandom)
      RandomModeService.OnLeave(steamId, timer, mode);

    RemovePawns(player, log);
    player.Remove();
    timer.Once(OrphanSweepSeconds.Seconds(), () => SweepOrphanObservers(mode));

    StatsService.RefreshBoards(mode);
    AutoStartService.Check(timer, mode, steamId);
  }

  // A pawn left without its client keeps queuing network changes the server cannot send
  // ("Couldn't resolve offset ... in CCitadelPlayerPawn") until it is removed.
  private static void RemovePawns(CCitadelPlayerController player, Logger log)
  {
    var hero = player.GetHeroPawn();
    var current = player.Pawn;
    var sameEntity = hero != null && current != null && hero.EntityHandle == current.EntityHandle;
    var extra = current != null && !sameEntity ? current : null;

    log.Info(
      player.ToPlayerRef(),
      "Disconnect pawns removed Hero={Hero} HeroIndex={HeroIndex} Current={Current} CurrentIndex={CurrentIndex} Removed={Removed}",
      hero?.DesignerName ?? "none",
      hero?.EntityIndex ?? -1,
      current?.DesignerName ?? "none",
      current?.EntityIndex ?? -1,
      (hero != null ? 1 : 0) + (extra != null ? 1 : 0));

    hero?.Remove();
    extra?.Remove();
  }

  public static void OnDisconnectWithoutController(int slot, ENetworkDisconnectionReason reason, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    LobbyLog.WithMode(mode).Warn("Disconnect without a controller Slot={Slot} Reason={Reason}", slot, reason);
    timer.Once(OrphanSweepSeconds.Seconds(), () => SweepOrphanObservers(mode));
  }

  // Only observer pawns: hero pawns lose their controller for a moment during a rebuild.
  public static int SweepOrphanObservers(ExecutionMode mode = ExecutionMode.Clean)
  {
    var owned = Players.GetAll()
      .Select(player => player.Pawn?.EntityHandle)
      .OfType<uint>()
      .ToHashSet();
    var removed = 0;

    foreach (var entity in Entities.ByDesignerName(SpectateService.ObserverDesignerName).ToList())
    {
      if (owned.Contains(entity.EntityHandle))
        continue;

      LobbyLog.WithMode(mode).Warn("Orphan observer pawn removed Index={Index} Class={Class}", entity.EntityIndex, entity.Classname);
      entity.Remove();
      removed++;
    }

    return removed;
  }

  public static bool KickPlayer(int slot, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = LobbyLog.WithMode(mode);
    var player = Players.GetAll().FirstOrDefault(player => player.Slot == slot);

    if (player == null)
    {
      log.Warn("Kick failed, no player in slot Slot={Slot}", slot);
      return false;
    }

    log.Info(player.ToPlayerRef(), "Kicking player");

    if (DraftState.Release(player.PlayerSteamId, out var hero))
    {
      log.Info(player.ToPlayerRef(), "Released pick before kick Hero={Hero}", hero);
      DraftService.RedrawBoards(mode);
    }

    Server.ExecuteCommand($"kickid {slot}");
    return true;
  }

  public static void LogDeath(CBasePlayerController player, CBasePlayerPawn pawn)
  {
    PlayersLog.Debug(
      player.ToPlayerRef(),
      "Player died LifeState={LifeState} Health={Health} Position={Position}",
      pawn.LifeState,
      pawn.Health,
      pawn.Position);
  }

  public static string DescribePlayer(CCitadelPlayerController player)
  {
    var pick = DraftState.TryGetPick(player.PlayerSteamId, out var hero) ? hero.ToString() : "-";
    var head =
      $"Slot={player.Slot} | Name={player.PlayerName} | SteamID={player.PlayerSteamId} | " +
      $"Team={RiftRouletteTeams.Name(player.TeamNum)} | Pick={pick}";

    var pawn = player.GetHeroPawn();

    if (pawn == null)
      return $"{head} | Pawn=NULL";

    return
      $"{head} | Hero={pawn.HeroID} | LifeState={pawn.LifeState} | IsAlive={pawn.IsAlive} | " +
      $"Health={pawn.Health} | MaxHealth={pawn.GetMaxHealth()} | Position={pawn.Position} | " +
      $"EntityIndex={pawn.EntityIndex}";
  }

  public static IReadOnlyList<string> ListPlayers() =>
    Players.GetAll().Select(DescribePlayer).ToList();

  public static bool SetTeam(CCitadelPlayerController player, int team, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = LobbyLog.WithMode(mode);

    if (DraftState.TryGetPick(player.PlayerSteamId, out var hero))
    {
      log.Info(player.ToPlayerRef(), "Team change refused, player has a pick Hero={Hero}", hero);
      return false;
    }

    player.ChangeTeam(team);
    log.Info(player.ToPlayerRef(), "Team changed Team={Team}", RiftRouletteTeams.Name(team));
    return true;
  }
}
