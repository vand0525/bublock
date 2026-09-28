using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Draft;
using RiftRoulette.GameLoop;
using RiftRoulette.Round;
using RiftRoulette.SelfTest;

namespace RiftRoulette.Lobby;

public class LobbyPlugin : DeadworksPluginBase
{
  private static readonly Logger LobbyLog = BublockLog.For("Lobby");

  private static readonly Logger PlayersLog = BublockLog.For("Players");

  private static readonly HashSet<string> SeenAbilities = [];

  private readonly List<IHandle> _pauseHooks = [];

  public override string Name => "Rift Roulette Lobby";

  public override void OnLoad(bool isReload)
  {
    if (isReload)
    {
      LobbyService.ApplyServerConvars();
      AdminSeat.Restore();
      BanStatueService.KickConnectedBanned();
    }

    HookPauseMessage<CCLCMsg_RequestPause>();
    HookPauseMessage<CCitadelClientMsg_Pause>();

    Timer.Every(StreamCam.TickSeconds.Seconds(), () => StreamCam.Tick(Timer));
    Timer.Every(AutoStartService.WaitingReminderSeconds.Seconds(), () => AutoStartService.RemindWaiting());
    Timer.Every(BanStatueService.SustainSeconds.Seconds(), BanStatueService.Sustain);
  }

  public override void OnUnload()
  {
    foreach (var hook in _pauseHooks)
      hook.Cancel();

    _pauseHooks.Clear();
  }

  public override void OnPrecacheResources()
  {
    Precache.AddHero(BanStatueService.StatueLookHero);
    LobbyLog.Info("Precached hero Hero={Hero}", BanStatueService.StatueLookHero);
  }

  public override void OnStartupServer()
  {
    LobbyService.ApplyServerConvars();
  }

  public override void OnGameFrame(bool simulating, bool firstTick, bool lastTick)
  {
    PauseGuard.Tick();
  }

  public override HookResult OnClientConCommand(ClientConCommandEvent args)
  {
    if (args.Controller is { } caller)
      StreamCam.OnAdminCommand(caller, args.Command, args.Args);

    if (!PauseRule.IsPauseCommand(args.Command) || !PauseGuard.Block(args.Controller, "command", args.Command))
      return HookResult.Continue;

    return HookResult.Stop;
  }

  private void HookPauseMessage<T>() where T : Google.Protobuf.IMessage<T>, new()
  {
    try
    {
      _pauseHooks.Add(NetMessages.HookIncoming<T>(context =>
        PauseGuard.Block(context.SenderSlot, "message", typeof(T).Name) ? HookResult.Stop : HookResult.Continue));
    }
    catch (InvalidOperationException exception)
    {
      LobbyLog.Warn("Pause message hook not registered Message={Message} Reason={Reason}", typeof(T).Name, exception.Message);
    }
  }

  public override bool OnClientConnect(ClientConnectEvent args)
  {
    EventCounters.Hit("client_connect");
    return AccessService.AllowConnect(args.SteamId, args.Name) && AdminSeat.AllowConnect(args.SteamId, args.Name);
  }

  public override void OnClientFullConnect(ClientFullConnectEvent args)
  {
    EventCounters.Hit("client_full_connect");
    var player = args.Controller;

    if (player == null)
      return;

    if (BanStatueService.TakeArrival(player.PlayerSteamId))
      BanStatueService.Petrify(player, BanStatueService.RejoinKickSeconds, liveBan: false, Timer);
    else if (AdminSeat.SeatOnJoin(player))
      AdminSeat.Sit(player, Timer);
    else
      LobbyService.AdmitPlayer(player, Timer);
  }

  public override void OnClientDisconnect(ClientDisconnectedEvent args)
  {
    EventCounters.Hit("client_disconnect");

    if (args.Controller != null)
      LobbyService.RemovePlayer(args.Controller, Timer);
  }

  [GameEventHandler("player_spawn")]
  public HookResult OnPlayerSpawn(PlayerSpawnEvent args)
  {
    EventCounters.Hit("player_spawn");
    var player = args.UseridController?.As<CCitadelPlayerController>();

    if (player == null || (!Participants.IsParticipant(player) && !BanStatueService.IsStatue(player.PlayerSteamId)))
      return HookResult.Continue;

    WatchGuard.Grace(player.PlayerSteamId);
    StreamCam.NoteSpawn(player.PlayerSteamId);
    Timer.NextTick(() => WatchSpot.SendUp(player));

    return HookResult.Continue;
  }

  [GameEventHandler("player_death")]
  public HookResult OnPlayerDeath(PlayerDeathEvent args)
  {
    EventCounters.Hit("player_death");
    var player = args.UseridController;
    var pawn = args.UseridPawn;

    if (player != null && pawn != null)
      LobbyService.LogDeath(player, pawn);

    if (player?.As<CCitadelPlayerController>() is { } victim)
      StreamCam.OnDeath(victim, args.AttackerController?.As<CCitadelPlayerController>(), Timer);

    return HookResult.Continue;
  }

  [GameEventHandler("player_used_ability")]
  public HookResult OnPlayerUsedAbility(PlayerUsedAbilityEvent args)
  {
    EventCounters.Hit("player_used_ability");
    var ability = args.Abilityname;
    var caster = (args.Player ?? args.Caster?.As<CBasePlayerPawn>())?.Controller?.As<CCitadelPlayerController>();

    if (SeenAbilities.Add(ability))
      LobbyLog.Info("Ability name seen for the first time since load Ability={Ability} Big={Big} Caster={Caster}", ability, BigUlts.IsBig(ability), caster?.PlayerName ?? "none");

    if (caster == null)
      return HookResult.Continue;

    LobbyLog.Trace(caster.ToPlayerRef(), "Ability used Ability={Ability} Big={Big}", ability, BigUlts.IsBig(ability));

    if (BigUlts.IsBig(ability) && Participants.IsParticipant(caster))
      StreamCam.OnBigUlt(caster, ability, Timer);

    return HookResult.Continue;
  }

  [Command("status", Description = "Show your slot, team, hero, pick, and health")]
  public void CmdStatus(CCitadelPlayerController player)
  {
    var line = LobbyService.DescribePlayer(player);

    PlayersLog.Info(player.ToPlayerRef(), "Status {Command} {Status}", "status", line);
    PlayerChat.Send(player, line);
  }

  [Command("commands", Description = "List the commands players can use")]
  public void CmdCommands(CCitadelPlayerController player)
  {
    foreach (var line in CommandList.PlayerCommands(typeof(LobbyPlugin).Assembly))
      PlayerChat.Send(player, line);

    PlayerChat.Send(player, CommandList.Footer);
  }

  [Command("player_list", Description = "List every player with team, hero, pick, and health")]
  public void CmdPlayerList(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, LobbyLog, "player_list");

    var lines = LobbyService.ListPlayers();
    AdminCommand.Reply(caller, $"[Lobby] {lines.Count} player(s)");

    foreach (var line in lines)
      AdminCommand.Reply(caller, $"[Lobby] {line}");
  }

  [Command("player_info", Description = "Show one player's status: player_info <slot>")]
  public void CmdPlayerInfo(CCitadelPlayerController? caller, int slot)
  {
    AdminCommand.Authorize(caller, LobbyLog, "player_info");

    var player = ResolvePlayer(slot);
    var line = LobbyService.DescribePlayer(player);

    PlayersLog.Info(player.ToPlayerRef(), "Status {Command} {Status}", "player_info", line);
    AdminCommand.Reply(caller, $"[Lobby] {line}");
  }

  [Command("player_kick", Description = "Kick a player and release their pick: player_kick <slot>")]
  public void CmdPlayerKick(CCitadelPlayerController? caller, int slot)
  {
    AdminCommand.Authorize(caller, LobbyLog, "player_kick");

    if (!LobbyService.KickPlayer(slot, ExecutionMode.Debug))
      throw new CommandException($"No player in slot {slot}.");

    AdminCommand.Reply(caller, $"[Lobby] Kicked slot {slot}");
  }

  [Command("player_team", Description = "Move a player without a pick to a team: player_team <slot> <sapphire|amber>")]
  public void CmdPlayerTeam(CCitadelPlayerController? caller, int slot, string team)
  {
    AdminCommand.Authorize(caller, LobbyLog, "player_team");

    if (!RiftRouletteTeams.TryParse(team, out var teamNumber))
      throw new CommandException("Team must be sapphire or amber.");

    var player = ResolvePlayer(slot);

    if (!LobbyService.SetTeam(player, teamNumber, ExecutionMode.Debug))
    {
      DraftState.TryGetPick(player.PlayerSteamId, out var hero);
      throw new CommandException($"{player.PlayerName} has picked {hero}; they must unpick first.");
    }

    AdminCommand.Reply(caller, $"[Lobby] Moved {player.PlayerName} to {RiftRouletteTeams.Name(teamNumber)}");
  }

  [Command("lobby_setup", Description = "Re-apply the Rift Roulette server convars")]
  public void CmdLobbySetup(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, LobbyLog, "lobby_setup");

    LobbyService.ApplyServerConvars(ExecutionMode.Debug);
    AdminCommand.Reply(caller, "[Lobby] Server convars applied");
  }

  [Command("pause_allow", Description = "Show whether players can pause, or set it: pause_allow [on|off]")]
  public void CmdPauseAllow(CCitadelPlayerController? caller, string state = "")
  {
    AdminCommand.Authorize(caller, LobbyLog, "pause_allow");

    switch (state.Trim().ToLowerInvariant())
    {
      case "":
        foreach (var line in PauseGuard.Describe())
          AdminCommand.Reply(caller, $"[Lobby] {line}");
        return;

      case "on" or "1":
        AdminCommand.Reply(caller, $"[Lobby] {PauseGuard.SetAllowed(true, ExecutionMode.Debug)}");
        return;

      case "off" or "0":
        AdminCommand.Reply(caller, $"[Lobby] {PauseGuard.SetAllowed(false, ExecutionMode.Debug)}");
        return;

      default:
        throw new CommandException("Usage: pause_allow [on|off]");
    }
  }

  [Command("seat_spec", ConsoleOnly = true, Description = "Admin seat: move the admin to spectator, any time; console only (dw_seat_spec)")]
  public void CmdSeatSpec(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, LobbyLog, "seat_spec");

    var player = SeatTarget(caller);
    AdminCommand.Reply(caller, $"[Lobby] {AdminSeat.Sit(player, Timer, ExecutionMode.Debug)}");
  }

  [Command("seat_play", Description = "Admin seat: move the admin from spectator onto a team (console: dw_seat_play)")]
  public void CmdSeatPlay(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, LobbyLog, "seat_play");

    var player = SeatTarget(caller);
    AdminCommand.Reply(caller, $"[Lobby] {AdminSeat.Stand(player, Timer, ExecutionMode.Debug)}");
  }

  [Command("seat_status", Description = "Show player slots, the admin seat, and maxplayers")]
  public void CmdSeatStatus(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, LobbyLog, "seat_status");

    foreach (var line in AdminSeat.Describe())
      AdminCommand.Reply(caller, $"[Lobby] {line}");
  }

  [Command("spec_auto", Description = "Stream camera: automatic follow / top-down on or off: spec_auto <on|off>")]
  public void CmdSpecAuto(CCitadelPlayerController? caller, string state)
  {
    AdminCommand.Authorize(caller, LobbyLog, "spec_auto");

    var on = state.Trim().ToLowerInvariant() switch
    {
      "on" or "1" => true,
      "off" or "0" => false,
      _ => throw new CommandException("Usage: spec_auto <on|off>")
    };

    var admin = SeatTarget(caller);
    StreamCam.SetAuto(admin, on, ExecutionMode.Debug);
    AdminCommand.Reply(caller, $"[Lobby] Stream camera auto {(on ? "on" : "off")}");
  }

  [Command("spec_status", Description = "Stream camera: who is on camera, top-down state, and the round")]
  public void CmdSpecStatus(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, LobbyLog, "spec_status");

    foreach (var line in StreamCam.Describe(SeatTarget(caller)))
      AdminCommand.Reply(caller, $"[Lobby] {line}");
  }

  [Command("spec_overview", Description = "Stream camera: show the top-down view over the rift now for 10 s")]
  public void CmdSpecOverview(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, LobbyLog, "spec_overview");

    AdminCommand.Reply(caller, $"[Lobby] {StreamCam.ShowOverview(SeatTarget(caller), Timer, ExecutionMode.Debug)}");
  }

  // A console command can arrive without a caller, so the seat falls back to the admin Steam ID.
  private static CCitadelPlayerController SeatTarget(CCitadelPlayerController? caller) =>
    caller
      ?? Players.GetAll().FirstOrDefault(player => AdminAuth.SteamIds.Contains(player.PlayerSteamId))
      ?? throw new CommandException("The admin is not connected.");

  private static CCitadelPlayerController ResolvePlayer(int slot) =>
    Players.GetAll().FirstOrDefault(player => player.Slot == slot)
      ?? throw new CommandException($"No player in slot {slot}.");
}
