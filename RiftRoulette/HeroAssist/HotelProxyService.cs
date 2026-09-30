using System.Numerics;
using Bublock.Modules.Restraint;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Lobby;
using RiftRoulette.Rift;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.HeroAssist;

public static class HotelProxyService
{
  public const string AbilityName = "ability_doorman_hotel";

  public const string BotName = "HotelProxy";

  public const Heroes LookHero = Heroes.Viper;

  public const float ModifierSeconds = 35f;

  public const double SetupDelaySeconds = 0.5;

  public const double SustainSeconds = 1.0;

  public const double VictimWaitSeconds = 0.75;

  private static readonly Logger Log = BublockLog.For("HeroAssist");

  private static int? _proxySlot;

  private static ulong? _victimSteamId;

  private static IHandle? _sustain;

  private static bool _modifierWarned;

  // Off until CreateFakeClient path is verified in playtest (crashed on first upload).
  public static bool Enabled { get; private set; }

  public static bool HasProxy => _proxySlot != null;

  public static string SetEnabled(bool enabled, ExecutionMode mode = ExecutionMode.Clean)
  {
    Enabled = enabled;
    Log.WithMode(mode).Info("Hotel proxy {State}", enabled ? "on" : "off");

    if (!enabled)
      Clear(mode);

    return $"Hotel proxy {(enabled ? "on" : "off")}.";
  }

  public static void OnHotelUsed(CCitadelPlayerController caster, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);

    if (!Enabled)
    {
      log.Debug(caster.ToPlayerRef(), "Hotel proxy skipped, off");
      return;
    }

    if (!RiftService.IsRunning || RiftService.CurrentSide is not { } side)
    {
      log.Debug(caster.ToPlayerRef(), "Hotel proxy skipped, no live rift");
      return;
    }

    if (!RiftRouletteTeams.IsPlayable(caster.TeamNum))
    {
      log.Debug(caster.ToPlayerRef(), "Hotel proxy skipped, caster not on a playable team");
      return;
    }

    if (HasProxy)
    {
      log.Debug(caster.ToPlayerRef(), "Hotel proxy skipped, already active");
      return;
    }

    var team = RiftRouletteTeams.Other(caster.TeamNum);
    var center = RiftSides.Position(side);

    timer.Once(VictimWaitSeconds.Seconds(), () =>
      BeginProxy(caster.PlayerSteamId, team, center, timer, mode));
  }

  public static void Clear(ExecutionMode mode = ExecutionMode.Clean)
  {
    _sustain?.Cancel();
    _sustain = null;
    _victimSteamId = null;
    _modifierWarned = false;

    if (_proxySlot is not int slot)
      return;

    _proxySlot = null;
    Server.Kick(slot);
    Log.WithMode(mode).Info("Hotel proxy kicked Slot={Slot}", slot);
  }

  public static bool IsProxyPawn(CBaseEntity? entity) =>
    _proxySlot is int slot &&
    entity?.As<CCitadelPlayerPawn>()?.Controller is { } controller &&
    controller.Slot == slot;

  public static string Describe() =>
    $"HotelProxy={(Enabled ? "on" : "off")} | Active={(HasProxy ? $"slot {_proxySlot}" : "no")}";

  private static void BeginProxy(
    ulong casterSteamId,
    int team,
    Vector3 center,
    ITimer timer,
    ExecutionMode mode)
  {
    var log = Log.WithMode(mode);

    if (!Enabled || HasProxy || !RiftService.IsRunning)
      return;

    var victim = FindHotelVictim(casterSteamId);

    if (victim == null)
    {
      log.Debug("Hotel proxy skipped, no victim in the hotel");
      return;
    }

    _victimSteamId = victim.PlayerSteamId;
    var slot = Server.CreateFakeClient(BotName);

    if (slot < 0)
    {
      _victimSteamId = null;
      log.Warn(victim.ToPlayerRef(), "Hotel proxy CreateFakeClient refused Slot={Slot}", slot);
      return;
    }

    _proxySlot = slot;
    log.Info(
      victim.ToPlayerRef(),
      "Hotel proxy spawning Slot={Slot} Team={Team} Center={Center}",
      slot,
      RiftRouletteTeams.Name(team),
      center);

    timer.Once(SetupDelaySeconds.Seconds(), () => SetupProxy(slot, team, center, timer, mode));
  }

  private static void SetupProxy(int slot, int team, Vector3 center, ITimer timer, ExecutionMode mode)
  {
    var log = Log.WithMode(mode);
    var bot = FindBySlot(slot);

    if (bot == null)
    {
      log.Warn("Hotel proxy controller missing Slot={Slot}", slot);
      Clear(mode);
      return;
    }

    bot.ChangeTeam(team, keepHero: false);
    bot.SelectHero(LookHero);

    timer.Once(SetupDelaySeconds.Seconds(), () =>
    {
      if (_proxySlot != slot)
        return;

      var current = FindBySlot(slot);
      var pawn = current?.GetHeroPawn();

      if (current == null || pawn == null || !pawn.IsAlive)
      {
        log.Warn("Hotel proxy pawn missing after hero swap Slot={Slot}", slot);
        Clear(mode);
        return;
      }

      pawn.Teleport(center, angles: null, velocity: Vector3.Zero);
      Hold(current);
      log.Info(
        current.ToPlayerRef(),
        "Hotel proxy live Team={Team} Position={Position}",
        RiftRouletteTeams.Name(current.TeamNum),
        center);

      _sustain = timer.Every(SustainSeconds.Seconds(), () => Sustain(slot, center, mode));
    });
  }

  private static void Sustain(int slot, Vector3 center, ExecutionMode mode)
  {
    if (_proxySlot != slot)
      return;

    if (_victimSteamId is ulong victimId && !IsInHotel(FindHuman(victimId)))
    {
      Log.WithMode(mode).Info("Hotel proxy ending, victim left the hotel");
      Clear(mode);
      return;
    }

    if (!RiftService.IsRunning)
    {
      Clear(mode);
      return;
    }

    var bot = FindBySlot(slot);
    var pawn = bot?.GetHeroPawn();

    if (bot == null || pawn == null || !pawn.IsAlive)
    {
      Clear(mode);
      return;
    }

    pawn.Teleport(center, angles: null, velocity: Vector3.Zero);
    Hold(bot);
  }

  private static void Hold(CCitadelPlayerController player)
  {
    var pawn = player.GetHeroPawn();

    if (pawn == null || !pawn.IsAlive || pawn.ModifierProp is not { } states)
      return;

    var modifier = AccessService.Load().StatueModifier;

    if (string.IsNullOrWhiteSpace(modifier))
    {
      if (!_modifierWarned)
      {
        _modifierWarned = true;
        Log.Warn(player.ToPlayerRef(), "Hotel proxy statue modifier not set - set it with /ban_modifier");
      }

      return;
    }

    if (states.HasModifier(modifier))
      return;

    if (!RestraintService.AddModifier(pawn, modifier, ModifierSeconds) && !_modifierWarned)
    {
      _modifierWarned = true;
      Log.Warn(player.ToPlayerRef(), "Hotel proxy statue modifier refused Modifier={Modifier}", modifier);
    }
  }

  private static CCitadelPlayerController? FindHotelVictim(ulong casterSteamId) =>
    Participants.Humans()
      .Where(player => player.PlayerSteamId != casterSteamId)
      .FirstOrDefault(IsInHotel);

  private static bool IsInHotel(CCitadelPlayerController? player) =>
    player?.GetHeroPawn()?.ModifierProp?.HasModifierState(EModifierState.InAlternateDimension) == true;

  private static CCitadelPlayerController? FindBySlot(int slot) =>
    Players.GetAllControllers().FirstOrDefault(player => player.Slot == slot);

  private static CCitadelPlayerController? FindHuman(ulong steamId) =>
    Participants.Humans().FirstOrDefault(player => player.PlayerSteamId == steamId);
}
