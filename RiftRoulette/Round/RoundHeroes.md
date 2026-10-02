# RoundHeroes

Which player (Steam ID) fights this round, and as which hero. Static, pure
data; no game calls, no logging. Random and Mirror mode write it each
intermission; `RoundFlow` moves only players with an entry into the rift
(grouped by `TeamNum`); everyone else stays up top.

## State

- `Steam ID -> Heroes` for this round's fighters.

## Operations

| Op | Behavior |
|---|---|
| `All`, `Count` | Every `(SteamId, Hero)` entry (live view) and how many |
| `Has(steamId)` / `TryGet(steamId, out hero)` | The player's round hero, if any |
| `Set(steamId, hero)` | Records or replaces the player's round hero. Several players may hold the same hero (Mirror mode) |
| `Remove(steamId, out hero)` | Drops the player's entry; false if they had none (disconnect, kick, admin seat, statue) |
| `Clear()` | Empties it (each new round, match end, mode change) |

## Invariants

- Keyed by Steam ID, not slot or entity index.
- Lives for one DLL load (static; a hot reload empties it).
- Game-thread only (not thread-safe).
