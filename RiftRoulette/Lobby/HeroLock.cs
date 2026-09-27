using Bublock.Shared;
using DeadworksManaged.Api;

namespace RiftRoulette.Lobby;

public sealed class HeroLock
{
  public const float EnforcementDamage = 1_000_000f;

  private readonly HashSet<ulong> applied = [];

  private readonly HashSet<ulong> pending = [];

  private readonly HashSet<ulong> kills = [];

  public int PendingCount => pending.Count;

  public bool IsPending(ulong steamId) => pending.Contains(steamId);

  public void MarkApplied(ulong steamId) => applied.Add(steamId);

  public void Unapply(ulong steamId) => applied.Remove(steamId);

  public void MarkPending(ulong steamId) => pending.Add(steamId);

  public bool ClearPending(ulong steamId) => pending.Remove(steamId);

  public void ClearAllPending() => pending.Clear();

  public bool ConsumeKill(ulong steamId) => kills.Remove(steamId);

  public void Enforce(
    CCitadelPlayerController player,
    CCitadelPlayerPawn pawn,
    Heroes assigned,
    string assignedName,
    Logger log,
    Action rebuildInPlace)
  {
    var steamId = player.PlayerSteamId;

    if (pawn.HeroID == assigned || !applied.Contains(steamId) || pending.Contains(steamId) || !pawn.IsAlive)
      return;

    applied.Remove(steamId);
    pending.Add(steamId);
    kills.Add(steamId);

    pawn.Hurt(EnforcementDamage);

    if (pawn.IsAlive && pawn.Health > 0)
    {
      kills.Remove(steamId);
      pending.Remove(steamId);
      log.Warn(player.ToPlayerRef(), "Hero swap kill failed, rebuilding in place Assigned={Assigned}", assigned);
      rebuildInPlace();
      return;
    }

    log.Info(
      player.ToPlayerRef(),
      "Hero swap punished, killed and rebuilt on respawn Current={Current} Assigned={Assigned}",
      pawn.HeroID,
      assigned);

    PlayerChat.Send(player, $"Changing hero is not allowed - you respawn as {assignedName}.");
  }

  public void Forget(ulong steamId)
  {
    applied.Remove(steamId);
    pending.Remove(steamId);
    kills.Remove(steamId);
  }

  public void Clear()
  {
    applied.Clear();
    pending.Clear();
    kills.Clear();
  }
}
