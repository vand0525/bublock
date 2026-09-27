using Bublock.Shared;
using DeadworksManaged.Api;

namespace CleanSlate;

public static class CleanSlateService
{
    private static readonly Logger CleanupLog = BublockLog.For("Cleanup");

    // Never add info_super_trooper_spawn (crash risk) or item_crate_spawn (urn spawn points the game picks from).
    public static readonly IReadOnlyList<string> RemovedDesignerNames =
    [
        "npc_trooper_boss",
        "npc_boss_tier2",
        "npc_barrack_boss",
        "citadel_item_powerup_spawner",
        "citadel_herotest_orbspawner",
        "citadel_shop_prop_dynamic"
    ];

    // Disabled, not removed: the shop system may still look these triggers up.
    public static readonly IReadOnlyList<string> DisabledDesignerNames =
    [
        "trigger_item_shop",
        "trigger_item_shop_safe_zone"
    ];

    public static void ApplyConvars(ExecutionMode mode = ExecutionMode.Clean)
    {
        // Disable trooper/NPC spawning
        ServerConVars.TrySet("citadel_trooper_spawn_enabled", 0, CleanupLog);
        ServerConVars.TrySet("citadel_npc_spawn_enabled", 0, CleanupLog);
        ServerConVars.TrySet("citadel_active_lane", 0, CleanupLog);

        // Prevent midboss from spawning
        ServerConVars.TrySet("citadel_midboss_initial_spawn_time_override", 999999, CleanupLog);

        // Prevent the urn (crate) from spawning
        ServerConVars.TrySet("citadel_crate_spawn_enabled", 0, CleanupLog);
        ServerConVars.TrySet("citadel_crate_disable_early_spawn", 1, CleanupLog);
        ServerConVars.TrySet("citadel_crate_spawn_initial_delay", 999999, CleanupLog);
        ServerConVars.TrySet("citadel_crate_respawn_interval", 999999, CleanupLog);

        CleanupLog.WithMode(mode).Debug("Spawn convars applied");
    }

    public static CleanupResult RemoveMapEntities(ExecutionMode mode = ExecutionMode.Clean)
    {
        var log = CleanupLog.WithMode(mode);
        var removed = new Dictionary<string, int>();
        var disabled = new Dictionary<string, int>();

        foreach (CBaseEntity entity in Entities.All.ToList())
        {
            var designerName = entity.DesignerName;

            if (RemovedDesignerNames.Contains(designerName))
            {
                log.Debug("Removing {DesignerName} {EntityName}", designerName, entity.Name);
                entity.Remove();
                Count(removed, designerName);
            }
            else if (DisabledDesignerNames.Contains(designerName))
            {
                log.Debug("Disabling {DesignerName} {EntityName}", designerName, entity.Name);
                entity.AcceptInput("Disable");
                Count(disabled, designerName);
            }
        }

        var result = new CleanupResult(removed, disabled);
        log.Info("Map entities cleaned {Summary}", result.Describe());
        return result;
    }

    private static void Count(Dictionary<string, int> counts, string designerName) =>
        counts[designerName] = counts.TryGetValue(designerName, out var count) ? count + 1 : 1;
}
