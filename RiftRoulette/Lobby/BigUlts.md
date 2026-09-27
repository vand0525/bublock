# BigUlts

The teamfight ultimates that send the stream camera top-down
(`StreamCam.OnBigUlt`). Pure data; tested by `BigUltsTests`.

## Data

`Heroes`: ability class name to hero name, case-insensitive. Class names are
each hero's `signature4` from `assets.deadlock-api.com/v2/heroes`
(2026-09-27).

| Hero | Ult | Class name |
|---|---|---|
| Lash | Death Slam | `citadel_ability_lash_ultimate` |
| Seven | Storm Cloud | `citadel_ability_storm_cloud` |
| Dynamo | Singularity | `citadel_ability_self_vacuum` |
| Kelvin | Frozen Shelter | `ability_ice_dome` |
| McGinnis | Heavy Barrage | `citadel_ability_rocket_barrage` |
| Haze | Bullet Dance | `ability_bullet_flurry` |
| Warden | Last Stand | `ability_warden_riot_protocol` |
| Bebop | Hyper Beam | `citadel_ability_bebop_laser_beam` |
| Abrams | Seismic Impact | `citadel_ability_bull_leap` |
| Infernus | Concussive Combustion | `ability_fire_bomb` |
| Pocket | Affliction | `synth_affliction` |
| Viscous | Goo Ball | `viscous_goo_bowling_ball` |
| Drifter | Eternal Night | `drifter_darkness` |
| Calico | Return to Shadows | `ability_nano_shadow_pulse` |
| Ivy | Air Drop | `citadel_ability_tengu_airlift` |
| Vyper | ult | `ability_viper_petrifybola` |
| Graves | ult | `ability_necro_gravestone` |

Left out (single-target or self-buff): Wraith, Shiv, Mo & Krill, Paradox,
Lady Geist, Doorman, Vindicta, Grey Talon, Holliday, Mina, Yamato, Silver,
Sinclair, Mirage, Venator, Victor, Paige, Billy, Apollo, Rem, Celeste.

## Operations

- `IsBig(abilityName)`: true for a listed name (trimmed, any case); false
  for null or empty.

## Invariants

- Adding or removing a hero is one line here plus the count in
  `BigUltsTests`.
- Heroes get reworked: after a game patch, check the first-sighting lines
  (`Ability name seen for the first time`) in `lobby-*.log` and
  `reference/patch-day.md`.
