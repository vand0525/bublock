using System.Numerics;
using Bublock.Shared;
using DeadworksManaged.Api;

namespace Bublock.Modules.MidBoss;

public static class MidBossService
{
  private static readonly Logger Log = BublockLog.For("MidBoss");

  private static readonly HashSet<uint> Bosses = [];
  private static readonly HashSet<uint> Crystals = [];

  private static SchemaAccessor<int> MaxHealthField => new("CBaseEntity"u8, "m_iMaxHealth"u8);

  private static readonly SchemaAccessor<bool>[] PickedUpFields =
  [
    new("CCitadelItemPickupRejuv"u8, "m_bPickedUp"u8),
    new("CCitadelItemPickup"u8, "m_bPickedUp"u8)
  ];

  private static (int Kills, int Amber, int Sapphire)? _counts;
  private static bool _firstScaledHitLogged;

  public static float DamageScale { get; private set; } = MidBossRule.DefaultScale;
  public static int HitCount { get; private set; }
  public static float DamageBefore { get; private set; }
  public static float DamageAfter { get; private set; }

  public static IReadOnlyList<CBaseEntity> LiveBosses() => Resolve(Bosses);

  public static IReadOnlyList<CBaseEntity> LiveCrystals() => Resolve(Crystals);

  // Hot reload and map start: tracking is rebuilt from what is on the map.
  public static void Rescan(ExecutionMode mode = ExecutionMode.Clean)
  {
    Bosses.Clear();
    Crystals.Clear();

    foreach (var entity in Entities.ByDesignerName(MidBossRule.BossName))
      Bosses.Add(entity.EntityHandle);

    foreach (var entity in Entities.ByDesignerName(MidBossRule.CrystalName))
      Crystals.Add(entity.EntityHandle);

    _counts = null;
    Log.WithMode(mode).Info("Tracking rebuilt Bosses={Bosses} Crystals={Crystals}", Bosses.Count, Crystals.Count);
  }

  public static void ObserveSpawned(CBaseEntity entity)
  {
    var designer = entity.DesignerName;

    if (MidBossRule.IsBoss(designer, entity.Classname))
    {
      if (Bosses.Add(entity.EntityHandle))
        BublockLog.Master.Info(
          "Mid-boss spawned Index={Index} Designer={Designer} Class={Class} Health={Health} MaxHealth={MaxHealth} Team={Team} Position={Position}",
          entity.EntityIndex, designer, entity.Classname, entity.Health, entity.MaxHealth, entity.TeamNum, Format(entity.Position));
    }
    else if (MidBossRule.IsCrystal(designer) && Crystals.Add(entity.EntityHandle))
    {
      BublockLog.Master.Info(
        "Crystal spawned Index={Index} Team={Team} Position={Position}",
        entity.EntityIndex, entity.TeamNum, Format(entity.Position));
    }
  }

  public static void ObserveDeleted(CBaseEntity entity)
  {
    var handle = entity.EntityHandle;

    if (Bosses.Remove(handle))
      Log.Info("Mid-boss deleted Index={Index} Health={Health}", entity.EntityIndex, entity.Health);
    else if (Crystals.Remove(handle))
      BublockLog.Master.Info(
        "Crystal removed Index={Index} PickedUp={PickedUp} Position={Position}",
        entity.EntityIndex, PickedUp(entity), Format(entity.Position));
  }

  public static void ObserveKilled(EntityKilledEvent args)
  {
    var victim = CBaseEntity.FromIndex(args.EntindexKilled);

    if (victim == null || !IsBoss(victim))
      return;

    var attacker = CBaseEntity.FromIndex(args.EntindexAttacker);
    var player = attacker?.As<CCitadelPlayerPawn>()?.Controller;
    var line = "Mid-boss killed Index={Index} Attacker={Attacker} Team={Team} Inflictor={Inflictor} DamageBits={DamageBits}";
    object?[] values =
    [
      args.EntindexKilled,
      player?.PlayerName ?? attacker?.DesignerName ?? "-",
      attacker?.TeamNum ?? 0,
      CBaseEntity.FromIndex(args.EntindexInflictor)?.DesignerName ?? "-",
      args.Damagebits
    ];

    if (player != null)
      BublockLog.Master.Info(player.ToPlayerRef(), line, values);
    else
      BublockLog.Master.Info(line, values);
  }

  public static void ObserveRespawnPending(float spawnTime) =>
    Log.Info("Mid-boss respawn pending SpawnTime={SpawnTime} GameClock={GameClock}", spawnTime, GameRules.GameClock);

  public static void ObserveRespawned() =>
    Log.Info("Mid-boss respawned GameClock={GameClock}", GameRules.GameClock);

  // Called every second: logs when the game's kill or crystal counts move.
  public static void WatchCounts()
  {
    if (!GameRules.IsValid)
      return;

    var now = (GameRules.MidbossKillCount, GameRules.AmberRejuvCount, GameRules.SapphireRejuvCount);

    if (_counts is { } before && before != now)
      BublockLog.Master.Info(
        "Mid-boss counts changed Kills={Kills} AmberRejuv={Amber} SapphireRejuv={Sapphire}",
        now.Item1, now.Item2, now.Item3);

    _counts = now;
  }

  public static bool IsBoss(CBaseEntity entity) =>
    Bosses.Contains(entity.EntityHandle) || MidBossRule.IsBoss(entity.DesignerName, entity.Classname);

  public static HookResult ScaleHit(TakeDamageEvent args)
  {
    if (!IsBoss(args.Entity))
      return HookResult.Continue;

    var before = args.Info.Damage;
    var after = MidBossRule.ScaleDamage(before, DamageScale);

    if (after != before)
      args.Info.Damage = after;

    HitCount++;
    DamageBefore += before;
    DamageAfter += after;

    if (!_firstScaledHitLogged)
    {
      _firstScaledHitLogged = true;
      Log.Info(
        "First hit on the mid-boss at this scale Scale={Scale} Before={Before} After={After} Flags={Flags} Health={Health}",
        DamageScale, before, after, args.Info.DamageFlags, args.Entity.Health);
    }

    return HookResult.Continue;
  }

  public static float SetDamageScale(float scale, ExecutionMode mode)
  {
    DamageScale = MidBossRule.ClampScale(scale);
    HitCount = 0;
    DamageBefore = 0f;
    DamageAfter = 0f;
    _firstScaledHitLogged = false;

    Log.WithMode(mode).Info("Damage scale set Requested={Requested} Scale={Scale}", scale, DamageScale);
    return DamageScale;
  }

  public static CBaseEntity Spawn(Vector3 position, ExecutionMode mode)
  {
    var log = Log.WithMode(mode);
    var entity = CBaseEntity.CreateByDesignerName(MidBossRule.BossName);

    if (entity == null)
    {
      log.Warn("Mid-boss create refused Designer={Designer}", MidBossRule.BossName);
      throw new CommandException($"The game refused to create {MidBossRule.BossName}.");
    }

    // The engine owns the keyvalues after Spawn; never dispose them.
    var keyValues = new CEntityKeyValues();
    keyValues.SetString("subclass_name", MidBossRule.BossName);
    keyValues.SetVector("origin", position);
    keyValues.SetVector("angles", Vector3.Zero);
    keyValues.SetInt("teamnumber", MidBossRule.NeutralTeam);

    log.Info("Mid-boss spawn requested Index={Index} Position={Position}", entity.EntityIndex, Format(position));
    entity.Spawn(keyValues);
    Bosses.Add(entity.EntityHandle);

    log.Info(
      "Mid-boss spawn returned Index={Index} Valid={Valid} Designer={Designer} Health={Health} MaxHealth={MaxHealth}",
      entity.EntityIndex, entity.IsValid, entity.DesignerName, entity.Health, entity.MaxHealth);

    return entity;
  }

  public static string Report(CBaseEntity entity) =>
    entity.IsValid
      ? $"#{entity.EntityIndex} {entity.DesignerName} alive={entity.IsAlive} health={entity.Health}/{entity.MaxHealth} (effective max {entity.GetMaxHealth()}) team={entity.TeamNum} at {Format(entity.Position)}"
      : "gone (no longer valid)";

  public static string Schedule(int seconds, ExecutionMode mode)
  {
    var set = ServerConVars.TrySet(MidBossRule.InitialSpawnConVar, seconds, Log);
    Log.WithMode(mode).Info(
      "Mid-boss scheduler set Seconds={Seconds} Applied={Applied} NpcSpawn={NpcSpawn} GameClock={GameClock} NextSpawn={NextSpawn}",
      seconds, set, ConVarText(MidBossRule.NpcSpawnConVar), GameRules.GameClock, GameRules.NextMidBossSpawnTime);

    var what = seconds == MidBossRule.ParkedSpawnSeconds ? "parked (999999)" : $"{seconds}s";
    return set
      ? $"{MidBossRule.InitialSpawnConVar} {what}; {MidBossRule.NpcSpawnConVar}={ConVarText(MidBossRule.NpcSpawnConVar)}, game clock {GameRules.GameClock:0}s."
      : $"{MidBossRule.InitialSpawnConVar} is missing; nothing changed.";
  }

  public static IReadOnlyList<string> SetHealth(int health, ExecutionMode mode) =>
    ForEachBoss(mode, "Health set", boss =>
    {
      boss.Health = health;
      return Report(boss);
    });

  public static IReadOnlyList<string> SetMaxHealth(int maxHealth, ExecutionMode mode) =>
    ForEachBoss(mode, "Max health set", boss =>
    {
      var field = MaxHealthField;

      if (field.GetAddress(boss.Handle) == boss.Handle)
        return "max health field missing (CBaseEntity.m_iMaxHealth)";

      field.Set(boss.Handle, maxHealth);
      return Report(boss);
    });

  public static IReadOnlyList<string> SetPercent(float percent, ExecutionMode mode) =>
    ForEachBoss(mode, "Health percent set", boss =>
    {
      boss.Health = MidBossRule.HealthAtPercent(boss.GetMaxHealth(), percent);
      return Report(boss);
    });

  public static IReadOnlyList<string> RemoveModifier(string name, ExecutionMode mode) =>
    ForEachBoss(mode, "Modifier remove", boss =>
      $"#{boss.EntityIndex} {name}: {(boss.RemoveModifier(name) ? "removed" : "not found")}");

  // Exact damage with ForceDeath, no resistances, and no OnTakeDamage call.
  public static IReadOnlyList<string> Kill(CBaseEntity? attacker, ExecutionMode mode) =>
    ForEachBoss(mode, "Kill", boss =>
    {
      using var info = new CTakeDamageInfo(Math.Max(boss.Health, 1) + 1f, attacker ?? boss, attacker ?? boss);
      info.DamageFlags |= TakeDamageFlags.ForceDeath;
      boss.TakeDamage(info);
      return Report(boss);
    });

  public static IReadOnlyList<string> Remove(ExecutionMode mode) =>
    ForEachBoss(mode, "Remove", boss =>
    {
      var index = boss.EntityIndex;
      boss.Remove();
      return $"#{index} removed";
    });

  public static IReadOnlyList<string> Describe()
  {
    var lines = new List<string>();
    var bosses = LiveBosses();

    lines.Add($"Bosses: {bosses.Count}");

    foreach (var boss in bosses)
    {
      lines.Add($"  {Report(boss)}");
      lines.Add($"    modifiers: {ModifierNames(boss)}");
    }

    if (GameRules.IsValid)
      lines.Add(
        $"Game: clock {GameRules.GameClock:0}s, next spawn {GameRules.NextMidBossSpawnTime:0}, kills {GameRules.MidbossKillCount}, " +
        $"crystals Amber {GameRules.AmberRejuvCount} / Sapphire {GameRules.SapphireRejuvCount}");
    else
      lines.Add("Game: game rules not found");

    lines.Add(
      $"Convars: {MidBossRule.InitialSpawnConVar}={ConVarText(MidBossRule.InitialSpawnConVar)}, " +
      $"{MidBossRule.SpawnIntervalConVar}={ConVarText(MidBossRule.SpawnIntervalConVar)}, " +
      $"{MidBossRule.NpcSpawnConVar}={ConVarText(MidBossRule.NpcSpawnConVar)}");

    lines.Add($"Damage scale: {DamageScale:0.##} (hits {HitCount}, damage {DamageBefore:0} -> {DamageAfter:0})");

    var crystals = LiveCrystals();
    lines.Add($"Crystals: {crystals.Count}");

    foreach (var crystal in crystals)
      lines.Add($"  #{crystal.EntityIndex} team={crystal.TeamNum} picked up={PickedUp(crystal)} at {Format(crystal.Position)}");

    return lines;
  }

  private static IReadOnlyList<string> ForEachBoss(ExecutionMode mode, string action, Func<CBaseEntity, string> apply)
  {
    var bosses = LiveBosses();
    var log = Log.WithMode(mode);

    if (bosses.Count == 0)
      throw new CommandException("No mid-boss is tracked. Spawn one or check /midboss_status.");

    var lines = new List<string>();

    foreach (var boss in bosses)
    {
      var index = boss.EntityIndex;
      var result = apply(boss);
      log.Info("{Action} Index={Index} Result={Result}", action, index, result);
      lines.Add(result);
    }

    return lines;
  }

  private static IReadOnlyList<CBaseEntity> Resolve(HashSet<uint> handles)
  {
    var live = new List<CBaseEntity>();

    foreach (var handle in handles.ToList())
    {
      var entity = CBaseEntity.FromHandle(handle);

      if (entity is { IsValid: true })
        live.Add(entity);
      else
        handles.Remove(handle);
    }

    return live;
  }

  private static string PickedUp(CBaseEntity crystal)
  {
    foreach (var field in PickedUpFields)
    {
      if (field.GetAddress(crystal.Handle) != crystal.Handle)
        return field.Get(crystal.Handle) ? "yes" : "no";
    }

    return "unknown";
  }

  private static string ModifierNames(CBaseEntity entity)
  {
    var names = entity.ModifierProp?.Modifiers
      .Select(modifier => modifier.SubclassVData?.Name)
      .Where(name => !string.IsNullOrEmpty(name))
      .ToList();

    return names is { Count: > 0 } ? string.Join(", ", names) : "none";
  }

  private static string ConVarText(string name) => ConVar.Find(name)?.GetInt().ToString() ?? "missing";

  private static string Format(Vector3 position) => $"({position.X:0}, {position.Y:0}, {position.Z:0})";
}
