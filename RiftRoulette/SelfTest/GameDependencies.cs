using RiftRoulette.Lobby;

namespace RiftRoulette.SelfTest;

public sealed record ConVarExpectation(string Name, int? Expected, CheckStatus IfMissing, string Owner);

public static class GameDependencies
{
  public const float RiftPointTolerance = 100f;

  public const string RiftPointName = "info_koth_spawn_location";

  public static IReadOnlyList<ConVarExpectation> ConVars { get; } =
  [
    new("citadel_team_size", 6, CheckStatus.Fail, "Lobby"),
    new("maxplayers", AdminSeatRule.PlayerCap + 1, CheckStatus.Warn, "Lobby; missing after engine 6712 (host may set it)"),
    new("sv_visiblemaxplayers", AdminSeatRule.PlayerCap, CheckStatus.Fail, "Lobby"),
    new("citadel_koth_enabled", null, CheckStatus.Fail, "Rift"),
    new("citadel_koth_warning_time", 1, CheckStatus.Warn, "Lobby, set through the console"),
    new("citadel_koth_early_warning_time", 1, CheckStatus.Warn, "Lobby, set through the console"),
    new("citadel_player_override_spawn_time", 1, CheckStatus.Warn, "Lobby, set through the console"),
    new("citadel_allow_duplicate_heroes", 1, CheckStatus.Fail, "Lobby"),
    new("citadel_hero_demo_unlock_flex_slots", 1, CheckStatus.Warn, "Lobby; missing after engine 6712 — FlexSlots schema write is the real unlock"),
    new("citadel_allow_purchasing_anywhere", null, CheckStatus.Fail, "GameLoop shop"),
    new("citadel_allow_pausing", 0, CheckStatus.Warn, "Lobby pause, 1 after /pause_allow on"),
    new("citadel_allow_pause_in_match", 0, CheckStatus.Warn, "Lobby pause, 1 after /pause_allow on"),
    new("citadel_pause_allow_in_pregame", 0, CheckStatus.Warn, "Lobby pause"),
    new("citadel_trooper_spawn_enabled", 0, CheckStatus.Fail, "CleanSlate"),
    new("citadel_npc_spawn_enabled", 0, CheckStatus.Fail, "CleanSlate"),
    new("citadel_active_lane", 0, CheckStatus.Warn, "CleanSlate"),
    new("citadel_midboss_initial_spawn_time_override", 999999, CheckStatus.Fail, "CleanSlate"),
    new("citadel_crate_spawn_enabled", 0, CheckStatus.Fail, "CleanSlate"),
    new("citadel_crate_disable_early_spawn", 1, CheckStatus.Warn, "CleanSlate, may not exist"),
    new("citadel_crate_spawn_initial_delay", 999999, CheckStatus.Fail, "CleanSlate"),
    new("citadel_crate_respawn_interval", 999999, CheckStatus.Fail, "CleanSlate"),
    new("citadel_voice_all_talk", 1, CheckStatus.Fail, "Lobby, all-team voice")
  ];

  public static IReadOnlyList<string> RequiredEntities { get; } = ["citadel_gamerules", "citadel_team_manager"];

  public static IReadOnlyList<string> KeptEntities { get; } =
  [
    "info_super_trooper_spawn",
    "item_crate_spawn",
    "trigger_item_shop",
    "trigger_item_shop_safe_zone",
    RiftPointName
  ];

  public static IReadOnlyList<string> CleanedEntities { get; } =
  [
    "npc_trooper_boss",
    "npc_boss_tier2",
    "npc_barrack_boss",
    "citadel_item_powerup_spawner",
    "citadel_herotest_orbspawner",
    "citadel_shop_prop_dynamic"
  ];

  public static IReadOnlyList<string> Events { get; } =
  [
    "player_spawn",
    "player_death",
    "player_respawned",
    "player_hero_changed",
    "client_connect",
    "client_full_connect",
    "client_disconnect",
    "game_frame",
    "modify_currency",
    "take_damage"
  ];
}
