# DamageLedger

Pure per-life damage totals: how much each attacker dealt to each victim
since the victim's last spawn. Decides who gets an assist. No game calls;
unit tested in `Tests/RiftRoulette.Tests`.

The game's own assist list (`player_death` `Assister1..5controller`)
credits players near the kill, including dead fighters watching from up
top, so it is not used.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Record(victimId, attackerId, amount)` | Adds `amount` to that attacker's total on that victim. Ignores zero IDs, self-damage and amounts of zero or less | — |
| `DamageTo(victimId)` | Attacker Steam ID to damage dealt this life (empty if none) | read-only map |
| `Assisters(victimId, killerId, victimMaxHealth, fraction = AssistFraction)` | Attackers whose total is at least `fraction * victimMaxHealth`, excluding the killer, most damage first. Empty when `victimMaxHealth <= 0` | Steam IDs |
| `Clear(victimId)` | Drops the victim's totals (new life, or the death was recorded) | — |
| `Forget(steamId)` | Drops the player as victim and as attacker (disconnect) | — |
| `Reset()` | Clears everything (match stats reset) | — |

`AssistFraction` = 0.2 (20% of the victim's max health).

## Invariants

- Team rules are not checked here: `StatsLedger.RecordDeath` drops
  assisters not on the killer's team.
- A total only grows within one life; `Clear` on spawn and on death keeps
  damage from an earlier life from counting.
