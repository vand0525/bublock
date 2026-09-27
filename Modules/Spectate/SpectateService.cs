using System.Numerics;
using Bublock.Modules.Movement;
using Bublock.Shared;
using DeadworksManaged.Api;
using ITimer = DeadworksManaged.Api.ITimer;

namespace Bublock.Modules.Spectate;

public static class SpectateService
{
  public const string ObserverDesignerName = "observer";

  // Source 2 OBS_MODE_ROAMING; CS2 uses spec_mode 4 for free cam. Deadlock's key for it is C.
  public const int FlyCamMode = 4;

  public const double TeleportDelaySeconds = 0.25;
  public const double AngleDelaySeconds = 0.5;
  public const double AngleRepeatSeconds = 1.0;

  // Wide on purpose: a small manual nudge in fly cam is not a failed park.
  public const float ParkTolerance = 1500f;

  private static readonly Logger Log = BublockLog.For("Spectate");

  public static CBasePlayerPawn? Observer(CCitadelPlayerController player) =>
    player.Pawn is { } pawn && pawn.DesignerName == ObserverDesignerName ? pawn : null;

  public static bool IsObserving(CCitadelPlayerController player) => Observer(player) != null;

  public static CBaseEntity? Current(CCitadelPlayerController player) => Observer(player)?.ObserverTarget;

  public static ObserverMode_t Mode(CCitadelPlayerController player) =>
    Observer(player)?.ObserverMode ?? ObserverMode_t.None;

  public static bool IsWatching(CCitadelPlayerController player, CBaseEntity? target) =>
    target != null && Current(player) is { } current && current.EntityHandle == target.EntityHandle;

  // Never IsValidObserverTarget: it rejects TeamNum 3, a playing team in Deadlock.
  public static bool Follow(CCitadelPlayerController player, CCitadelPlayerController target, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var observer = Observer(player);
    var pawn = target.GetHeroPawn();

    if (observer == null || pawn == null)
    {
      log.Debug(player.ToPlayerRef(), "Follow skipped Observer={Observer} TargetPawn={TargetPawn}", observer != null, pawn != null);
      return false;
    }

    observer.SetObserverMode(ObserverMode_t.InEye);

    if (observer.SetObserverTarget(pawn))
    {
      log.Debug(player.ToPlayerRef(), "Following Target={Target} TargetSlot={TargetSlot}", target.PlayerName, target.Slot);
      return true;
    }

    ClientFollow(player, target, mode);
    return false;
  }

  public static void ClientFollow(CCitadelPlayerController player, CCitadelPlayerController target, ExecutionMode mode = ExecutionMode.Clean)
  {
    Server.ClientCommand(player.Slot, $"spec_player {target.Slot}");
    Log.WithMode(mode).Info(player.ToPlayerRef(), "Server target refused, sent client command Command={Command}", $"spec_player {target.Slot}");
  }

  // The server's Roaming mode does not switch the client; the client must be in fly cam before a teleport moves its camera.
  public static void SetFlyCam(CCitadelPlayerController player, ExecutionMode mode = ExecutionMode.Clean)
  {
    Server.ClientCommand(player.Slot, $"spec_mode {FlyCamMode}");
    Log.WithMode(mode).Debug(player.ToPlayerRef(), "Sent client command Command={Command}", $"spec_mode {FlyCamMode}");
  }

  // Order matters: fly cam, then teleport, then the angle a moment later (an angle sent with the teleport is ignored).
  public static bool Park(CCitadelPlayerController player, Vector3 position, Vector3 angle, ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var observer = Observer(player);

    if (observer == null)
    {
      log.Debug(player.ToPlayerRef(), "Park skipped, not an observer");
      return false;
    }

    var steamId = player.PlayerSteamId;

    observer.SetObserverMode(ObserverMode_t.Roaming);
    SetFlyCam(player, mode);

    timer.Once(TeleportDelaySeconds.Seconds(), () =>
    {
      if (Find(steamId) is { } again && Observer(again) is { } pawn)
        pawn.Teleport(position: position, angles: null, velocity: Vector3.Zero);
    });

    timer.Once(AngleDelaySeconds.Seconds(), () =>
    {
      if (Find(steamId) is { } again && IsObserving(again))
        MovementService.SetViewAngle(again, angle);
    });

    timer.Once(AngleRepeatSeconds.Seconds(), () =>
    {
      if (Find(steamId) is not { } again || Observer(again) is not { } pawn)
        return;

      MovementService.SetViewAngle(again, angle);
      log.Debug(again.ToPlayerRef(), "Parked Position={Position} Angle={Angle} After={After} Mode={Mode}", position, angle, pawn.Position, pawn.ObserverMode);
    });

    log.Debug(player.ToPlayerRef(), "Park started Position={Position} Angle={Angle}", position, angle);
    return true;
  }

  public static bool IsParkedAt(CCitadelPlayerController player, Vector3 position, float tolerance = ParkTolerance)
  {
    var observer = Observer(player);

    return observer != null && SpectateRule.ParkCheck(
      observer.ObserverMode == ObserverMode_t.Roaming,
      observer.ObserverTarget != null,
      Vector3.Distance(observer.Position, position),
      tolerance);
  }

  private static CCitadelPlayerController? Find(ulong steamId) =>
    Players.GetAll().FirstOrDefault(player => player.PlayerSteamId == steamId);
}
