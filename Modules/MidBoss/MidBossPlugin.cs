using System.Globalization;
using System.Numerics;
using Bublock.Shared;
using DeadworksManaged.Api;

namespace Bublock.Modules.MidBoss;

public class MidBossPlugin : DeadworksPluginBase
{
  private static readonly Logger CommandsLog = BublockLog.For("MidBoss");

  public override string Name => "MidBoss";

  public override void OnLoad(bool isReload)
  {
    MidBossService.Rescan();
    Timer.Every(1.Seconds(), MidBossService.WatchCounts);
  }

  public override void OnStartupServer() => MidBossService.Rescan();

  public override void OnEntitySpawned(EntitySpawnedEvent args) => MidBossService.ObserveSpawned(args.Entity);

  public override void OnEntityDeleted(EntityDeletedEvent args) => MidBossService.ObserveDeleted(args.Entity);

  public override HookResult OnTakeDamage(TakeDamageEvent args) => MidBossService.ScaleHit(args);

  [GameEventHandler("entity_killed")]
  public HookResult OnEntityKilled(EntityKilledEvent args)
  {
    MidBossService.ObserveKilled(args);
    return HookResult.Continue;
  }

  [GameEventHandler("midboss_respawn_pending")]
  public HookResult OnRespawnPending(MidbossRespawnPendingEvent args)
  {
    MidBossService.ObserveRespawnPending(args.SpawnTime);
    return HookResult.Continue;
  }

  [GameEventHandler("midboss_respawned")]
  public HookResult OnRespawned(GameEvent args)
  {
    MidBossService.ObserveRespawned();
    return HookResult.Continue;
  }

  [Command("midboss_status", Description = "Show every mid-boss, its health and modifiers, the spawn convars, damage scale and crystals")]
  public void CmdStatus(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, CommandsLog, "midboss_status");

    foreach (var line in MidBossService.Describe())
      AdminCommand.Reply(caller, $"[MidBoss] {line}");
  }

  [Command("midboss_spawn", Description = "Create a mid-boss at the pit, where you stand, or at a point: midboss_spawn [here | x y z]")]
  public void CmdSpawn(CCitadelPlayerController? caller, string at = "", float y = 0f, float z = 0f)
  {
    AdminCommand.Authorize(caller, CommandsLog, "midboss_spawn");

    var position = SpawnPosition(caller, at, y, z);
    var boss = MidBossService.Spawn(position, ExecutionMode.Debug);
    AdminCommand.Reply(caller, $"[MidBoss] Spawned: {MidBossService.Report(boss)}");

    Timer.Once(1.Seconds(), () =>
    {
      var report = MidBossService.Report(boss);
      CommandsLog.WithMode(ExecutionMode.Debug).Info("Mid-boss 1 s after spawn Report={Report}", report);

      if (caller == null || caller.IsValid)
        AdminCommand.Reply(caller, $"[MidBoss] 1 s later: {report}");
    });
  }

  [Command("midboss_schedule", Description = "Let the game spawn its own mid-boss: midboss_schedule <seconds|off>")]
  public void CmdSchedule(CCitadelPlayerController? caller, string seconds)
  {
    AdminCommand.Authorize(caller, CommandsLog, "midboss_schedule");

    if (!MidBossRule.TryParseSchedule(seconds, out var value))
      throw new CommandException($"'{seconds}' is not whole seconds (0-{MidBossRule.ParkedSpawnSeconds}) or 'off'.");

    AdminCommand.Reply(caller, $"[MidBoss] {MidBossService.Schedule(value, ExecutionMode.Debug)}");
  }

  [Command("midboss_health", Description = "Set every mid-boss's health: midboss_health <hp>")]
  public void CmdHealth(CCitadelPlayerController? caller, int health)
  {
    AdminCommand.Authorize(caller, CommandsLog, "midboss_health");

    Reply(caller, MidBossService.SetHealth(ValidHealth(health), ExecutionMode.Debug));
  }

  [Command("midboss_maxhealth", Description = "Set every mid-boss's max health: midboss_maxhealth <hp>")]
  public void CmdMaxHealth(CCitadelPlayerController? caller, int health)
  {
    AdminCommand.Authorize(caller, CommandsLog, "midboss_maxhealth");

    Reply(caller, MidBossService.SetMaxHealth(ValidHealth(health), ExecutionMode.Debug));
  }

  [Command("midboss_percent", Description = "Set every mid-boss's health to a percent of its max: midboss_percent <0-100>")]
  public void CmdPercent(CCitadelPlayerController? caller, float percent)
  {
    AdminCommand.Authorize(caller, CommandsLog, "midboss_percent");

    if (!float.IsFinite(percent) || percent <= 0f || percent > 100f)
      throw new CommandException("Percent must be above 0 and at most 100.");

    Reply(caller, MidBossService.SetPercent(percent, ExecutionMode.Debug));
  }

  [Command("midboss_damage_scale", Description = "Multiply damage dealt to the mid-boss: midboss_damage_scale <0.1-10>")]
  public void CmdDamageScale(CCitadelPlayerController? caller, float scale)
  {
    AdminCommand.Authorize(caller, CommandsLog, "midboss_damage_scale");

    var applied = MidBossService.SetDamageScale(scale, ExecutionMode.Debug);
    AdminCommand.Reply(caller, $"[MidBoss] Damage scale {applied:0.##} (hit counters reset)");
  }

  [Command("midboss_modifier_remove", Description = "Remove a modifier from every mid-boss by name: midboss_modifier_remove <name>")]
  public void CmdModifierRemove(CCitadelPlayerController? caller, string name)
  {
    AdminCommand.Authorize(caller, CommandsLog, "midboss_modifier_remove");

    Reply(caller, MidBossService.RemoveModifier(name.Trim(), ExecutionMode.Debug));
  }

  [Command("midboss_ready", Description = "Replace the mid-boss with a killable one sized for the souls per player: midboss_ready <souls> [fighters]")]
  public void CmdReady(CCitadelPlayerController? caller, int souls, int fighters = 0)
  {
    AdminCommand.Authorize(caller, CommandsLog, "midboss_ready");

    if (!MidBossRule.IsValidSouls(souls))
      throw new CommandException($"Souls must be 0-{MidBossRule.MaxSouls:N0}.");

    if (fighters < 0)
      throw new CommandException("Fighters must be 1 or more (leave it out to count players on a team).");

    var ready = MidBossService.Ready(souls, fighters > 0 ? fighters : null, ExecutionMode.Debug);
    var who = ready.FightersCounted ? "counted" : "given";

    AdminCommand.Reply(
      caller,
      $"[MidBoss] Ready #{ready.Boss.EntityIndex}: {ready.Souls:N0} souls per player = minute {ready.Minute:0.#} of a real match" +
      (ready.Replaced > 0 ? $" (replaced {ready.Replaced})" : ""));
    AdminCommand.Reply(
      caller,
      $"[MidBoss] Max {ready.MaxHealth:N0}{(ready.MaxWritten ? "" : " (max field missing, not written)")}, " +
      $"health {ready.Health:N0} ({MidBossRule.ReadyPercent:0}%), resistance {ready.Resistance}, " +
      $"damage x{ready.Scale:0.##} for {ready.Fighters} fighters ({who})");

    Timer.Once(1.Seconds(), () =>
    {
      var report = MidBossService.Recheck(ready, ExecutionMode.Debug);

      if (caller == null || caller.IsValid)
        AdminCommand.Reply(caller, $"[MidBoss] 1 s later: {report}");
    });
  }

  [Command("midboss_remove", Description = "Remove every mid-boss without a death (no crystal)")]
  public void CmdRemove(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, CommandsLog, "midboss_remove");

    Reply(caller, MidBossService.Remove(ExecutionMode.Debug));
  }

  private static Vector3 SpawnPosition(CCitadelPlayerController? caller, string at, float y, float z)
  {
    var text = at.Trim();

    if (text.Length == 0)
      return MidBossRule.CampSpot;

    if (text.Equals("here", StringComparison.OrdinalIgnoreCase))
    {
      var pawn = caller?.GetHeroPawn();
      return pawn is { IsValid: true }
        ? pawn.Position
        : throw new CommandException("'here' needs your hero in the world.");
    }

    return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
      ? new Vector3(x, y, z)
      : throw new CommandException($"'{text}' is not 'here' or an x coordinate.");
  }

  private static int ValidHealth(int health) =>
    MidBossRule.IsValidHealth(health)
      ? health
      : throw new CommandException($"Health must be 1-{MidBossRule.MaxHealthLimit}.");

  private static void Reply(CCitadelPlayerController? caller, IReadOnlyList<string> lines)
  {
    foreach (var line in lines)
      AdminCommand.Reply(caller, $"[MidBoss] {line}");
  }
}
