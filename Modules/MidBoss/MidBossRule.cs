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

  public const string ResistanceModifier = "npc_super_neutral/midboss_modifier_damage_resistance";

  // Given to whoever takes the crystal: the credit brings them back once after a death.
  public const string RebirthModifier = "citadel_item_pickup_rejuv/modifier_citadel_rebirth_credit";
  public const string RejuvEffectsModifier = "ability_item_pickup_effects/rejuv_effects";

  public const int AmberTeam = 2;
  public const int SapphireTeam = 3;

  // npc_units.vdata npc_super_neutral: a boss spawned at game minute m gets 13,000 + 195 * m (m rounded).
  public const int StartingHealth = 13000;
  public const int HealthPerMinute = 195;

  // Just above the 50% scream.
  public const float ReadyPercent = 51f;

  // 6v6 hits at 1x; fewer fighters hit proportionally harder.
  public const int BaselineFighters = 12;

  public const int MaxSouls = 1_000_000;

  // Median game minute at which a real ranked lobby's average net worth per player reaches the souls
  // (scripts/midboss-health.py, 1,000 matches, 2026-10-13). Copy a new run in by hand.
  public static readonly (int Souls, float Minute)[] MinuteBySouls =
  [
    (0, 0f), (2500, 4.1f), (5000, 7.5f), (7500, 10.2f), (10000, 12.6f), (12500, 14.8f), (15000, 17.0f),
    (17500, 19.1f), (20000, 21.1f), (22500, 23.0f), (25000, 24.9f), (27500, 26.7f), (30000, 28.4f),
    (32500, 30.2f), (35000, 31.9f), (37500, 33.7f), (40000, 35.4f), (42500, 37.1f), (45000, 38.9f)
  ];

  public static readonly Vector3 CampSpot = new(0f, 0f, -768f);

  public static bool IsBoss(string designerName, string className) =>
    designerName == BossName || className == BossName || className == BossClass;

  public static bool IsCrystal(string designerName) => designerName == CrystalName;

  public static float ClampScale(float scale) =>
    float.IsFinite(scale) ? Math.Clamp(scale, MinScale, MaxScale) : DefaultScale;

  public static float ScaleDamage(float damage, float scale) => damage * ClampScale(scale);

  // Never 0: a boss at 0 health is dead; killing is the players' job.
  public static int HealthAtPercent(int maxHealth, float percent)
  {
    var clamped = float.IsFinite(percent) ? Math.Clamp(percent, 0f, 100f) : 100f;
    var health = (int)Math.Round(Math.Max(maxHealth, 1) * clamped / 100f, MidpointRounding.AwayFromZero);
    return Math.Max(health, 1);
  }

  public static bool IsValidHealth(int health) => health >= 1 && health <= MaxHealthLimit;

  public static bool IsValidSouls(int souls) => souls >= 0 && souls <= MaxSouls;

  // The team whose rejuv count went up between two reads, Amber first; 0 when neither did.
  public static int CrystalTeam((int Amber, int Sapphire) before, (int Amber, int Sapphire) now) =>
    now.Amber > before.Amber ? AmberTeam
    : now.Sapphire > before.Sapphire ? SapphireTeam
    : 0;

  // Linear between table points; past the last point the last segment's slope continues.
  public static float MinuteForSouls(int souls)
  {
    var table = MinuteBySouls;

    if (souls <= table[0].Souls)
      return table[0].Minute;

    for (var i = 1; i < table.Length; i++)
    {
      if (souls <= table[i].Souls)
        return Lerp(table[i - 1], table[i], souls);
    }

    return Lerp(table[^2], table[^1], souls);
  }

  public static int HealthAtMinute(float minute)
  {
    var whole = Math.Round(Math.Max(minute, 0f), MidpointRounding.AwayFromZero);
    return (int)Math.Min(StartingHealth + HealthPerMinute * whole, MaxHealthLimit);
  }

  public static int HealthForSouls(int souls) => HealthAtMinute(MinuteForSouls(souls));

  public static float ScaleForFighters(int fighters) =>
    ClampScale((float)BaselineFighters / Math.Max(fighters, 1));

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

  private static float Lerp((int Souls, float Minute) from, (int Souls, float Minute) to, int souls) =>
    from.Minute + (to.Minute - from.Minute) * (souls - from.Souls) / (to.Souls - from.Souls);
}
