namespace RiftRoulette.Lobby;

public static class BigUlts
{
  // Class names are each hero's signature4 from assets.deadlock-api.com/v2/heroes (2026-09-27).
  public static readonly IReadOnlyDictionary<string, string> Heroes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
  {
    ["citadel_ability_lash_ultimate"] = "Lash",
    ["citadel_ability_storm_cloud"] = "Seven",
    ["citadel_ability_self_vacuum"] = "Dynamo",
    ["ability_ice_dome"] = "Kelvin",
    ["citadel_ability_rocket_barrage"] = "McGinnis",
    ["ability_bullet_flurry"] = "Haze",
    ["ability_warden_riot_protocol"] = "Warden",
    ["citadel_ability_bebop_laser_beam"] = "Bebop",
    ["citadel_ability_bull_leap"] = "Abrams",
    ["ability_fire_bomb"] = "Infernus",
    ["synth_affliction"] = "Pocket",
    ["viscous_goo_bowling_ball"] = "Viscous",
    ["drifter_darkness"] = "Drifter",
    ["ability_nano_shadow_pulse"] = "Calico",
    ["citadel_ability_tengu_airlift"] = "Ivy",
    ["ability_viper_petrifybola"] = "Vyper",
    ["ability_necro_gravestone"] = "Graves",
  };

  public static bool IsBig(string? abilityName) =>
    !string.IsNullOrWhiteSpace(abilityName) && Heroes.ContainsKey(abilityName.Trim());
}
