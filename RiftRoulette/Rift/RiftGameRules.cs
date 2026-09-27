using System.Numerics;
using Bublock.Shared;
using DeadworksManaged.Api;

namespace RiftRoulette.Rift;

public static class RiftGameRules
{
  public const float ParkedTime = 999999f;

  private static readonly Logger Log = BublockLog.For("Rift");

  public static SchemaAccessor<Vector3> NextLocation => new(
    "CCitadelGameRules"u8,
    "m_vNextKothLocation"u8);

  public static SchemaAccessor<float> NextWindow => new(
    "CCitadelGameRules"u8,
    "m_timeNextKothSpawnWindowTime"u8);

  public static SchemaAccessor<float> NextSpawn => new(
    "CCitadelGameRules"u8,
    "m_timeNextKothSpawn"u8);

  public static SchemaAccessor<float> KothGiveUp => new(
    "CCitadelGameRules"u8,
    "m_timeKothGiveUp"u8);

  public enum ResolveResult
  {
    Found,
    ProxyMissing,
    PointerNull
  }

  public static ResolveResult TryResolve(out nint gameRules)
  {
    gameRules = nint.Zero;

    var proxy = Entities
      .ByDesignerName("citadel_gamerules")
      .FirstOrDefault();

    if (proxy == null)
      return ResolveResult.ProxyMissing;

    gameRules = proxy.GetField<nint>(
      "CCitadelGameRulesProxy"u8,
      "m_pGameRules"u8);

    return gameRules == nint.Zero ? ResolveResult.PointerNull : ResolveResult.Found;
  }

  public static void ConfigureNextRift(nint gameRules, Vector3 position)
  {
    SetKothEnabled(false);

    NextLocation.Set(gameRules, position);
    NextWindow.Set(gameRules, 0f);
    NextSpawn.Set(gameRules, 0f);

    SetKothEnabled(true);
  }

  public static void ParkScheduler(nint gameRules)
  {
    NextWindow.Set(gameRules, ParkedTime);
    NextSpawn.Set(gameRules, ParkedTime);

    SetKothEnabled(false);
  }

  public static void SetKothEnabled(bool enabled)
  {
    ServerConVars.TrySet("citadel_koth_enabled", enabled ? 1 : 0, Log);
  }
}
