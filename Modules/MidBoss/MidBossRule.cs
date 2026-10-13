using System.Globalization;
using System.Numerics;

namespace Bublock.Modules.MidBoss;

public static class MidBossRule
{
  public const string BossName = "npc_super_neutral";
  public const string BossClass = "CNPC_MidBoss";
  public const string CrystalName = "citadel_item_pickup_rejuv";

  public const string InitialSpawnConVar = "citadel_midboss_initial_spawn_time_override";
  public const string SpawnIntervalConVar = "citadel_midboss_spawn_interval_override";
  public const string NpcSpawnConVar = "citadel_npc_spawn_enabled";
  public const int ParkedSpawnSeconds = 999999;

  public const int NeutralTeam = 4;

  public const float DefaultScale = 1f;
  public const float MinScale = 0.1f;
  public const float MaxScale = 10f;

  public const int MaxHealthLimit = 1_000_000;

  public static readonly Vector3 CampSpot = new(0f, 0f, -768f);

  public static bool IsBoss(string designerName, string className) =>
    designerName == BossName || className == BossName || className == BossClass;

  public static bool IsCrystal(string designerName) => designerName == CrystalName;

  public static float ClampScale(float scale) =>
    float.IsFinite(scale) ? Math.Clamp(scale, MinScale, MaxScale) : DefaultScale;

  public static float ScaleDamage(float damage, float scale) => damage * ClampScale(scale);

  // Never 0: a boss at 0 health is dead, and killing is /midboss_kill's job.
  public static int HealthAtPercent(int maxHealth, float percent)
  {
    var clamped = float.IsFinite(percent) ? Math.Clamp(percent, 0f, 100f) : 100f;
    var health = (int)Math.Round(Math.Max(maxHealth, 1) * clamped / 100f, MidpointRounding.AwayFromZero);
    return Math.Max(health, 1);
  }

  public static bool IsValidHealth(int health) => health >= 1 && health <= MaxHealthLimit;

  // "off" parks the scheduler; otherwise whole seconds of game time, 0 or more.
  public static bool TryParseSchedule(string text, out int seconds)
  {
    var trimmed = text.Trim();

    if (trimmed.Equals("off", StringComparison.OrdinalIgnoreCase))
    {
      seconds = ParkedSpawnSeconds;
      return true;
    }

    return int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds)
      && seconds >= 0
      && seconds <= ParkedSpawnSeconds;
  }
}
