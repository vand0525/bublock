# FlexSlots

Opens every flex item slot for both teams, so every hero holds 12 items
(9 are open by default). Static; the only state is a flag that keeps the
"no team entity" Warning to one per miss streak.

## Operations

| Member | Behavior |
|---|---|
| `TeamManager` | `citadel_team_manager`, the designer name of the team entities (`CCitadelTeam`) |
| `AllUnlocked` | 15: all four `EFlexSlotTypes_t` flags (`Slot01` 1, `Slot02` 2, `Slot03` 4, `Slot04` 8) |
| `Unlocked` | `SchemaAccessor<ushort>` for `CCitadelTeam.m_nFlexSlotsUnlocked` (networked) |
| `UnlockAll(mode)` | Every `citadel_team_manager` on team 2 (Amber) or 3 (Sapphire): if the field is not 15, sets it to 15 and logs `Flex slots unlocked Team= Before= After=` (Info, `lobby` log). Returns the number of teams found. No team entity: one Warning until a later call finds them, returns 0. Field missing from the schema (`GetAddress` returns the entity pointer): Warning, no write, returns 0 |
| `Describe()` | One line per `citadel_team_manager` (every team number) with its flags, or why it can't read them. Read only |

## Callers

- `LobbyService.ApplyServerConvars` (startup, hot reload, `/lobby_setup`).
- `LobbyService.AdmitPlayer` (every join; the teams exist by then).
- `MatchService.ScheduleNextRound` (every intermission, before builds are
  handed out).
- `/lobby_flex` (Debug).

## Dangerous constraints

- Writes game memory straight through the schema offset. Only entities
  with the exact designer name `citadel_team_manager` are written, and
  never when the field offset resolves to 0.
- `citadel_hero_demo_unlock_flex_slots 1` lets the server hold 12 items,
  but the client HUD draws the extra slots from this team field (0 until
  written). The HUD picks the new value up when the hero is rebuilt (every
  loadout does `ResetHero`), not on a live hero. The gamerules flag
  `CCitadelGameRules.m_bFlexSlotsForcedUnlocked` stays 0 and is not needed.
- Game-thread only.
