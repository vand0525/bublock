# RIFT RUMBLE — CHAT HANDOFF CONTEXT
> Note (2026-09-27): the game mode was renamed Rift Roulette (`RiftRoulette.dll`, `Bublock/RiftRoulette/`). This narrative is kept as written and uses the old name.

Last updated: 2026-09-26
This is context from a previous long development conversation. Treat the
"CURRENT DIRECTION / NEXT DISCUSSION" section near the bottom as the main task.
The next discussion is primarily about REFACTORING AND ARCHITECTURE, not
immediately adding more gameplay features.
---
# 1. PROJECT OVERVIEW
Project: Rift Rumble
Rift Rumble is a custom Deadlock game mode implemented as a C# plugin using
Deadworks.
Development/testing currently happens against a hosted Deadworks server.
The plugin has grown organically while figuring out how Deadlock/Deadworks
works. A lot of experimental functionality currently lives together, and the
next major goal is to refactor it into a clean, understandable architecture
before continuing to build the full game mode.
Important development preference:
- Do not invent Deadworks APIs.
- Verify uncertain APIs/schema fields before suggesting code.
- Make isolated changes when debugging.
- Preserve known-good behavior.
- Prefer one focused test at a time.
- For small code changes, show the exact insertion/replacement location rather
  than rewriting entire working methods.
- Search current Deadworks/Deadlock documentation/schema when necessary.
- Logs should use `[RiftRumble]`, not the old `[Rift Wars]` prefix.
---
# 2. HIGH-LEVEL RIFT RUMBLE GAME MODE
Current concept:
1. Players enter a draft/staging area.
2. A predefined set of heroes is available to Sapphire and Amber.
3. Players select heroes.
4. Players are assigned the corresponding hero/team.
5. Once setup is complete, a Rift round begins.
6. A real Deadlock Rift is spawned at either the Green or Yellow Rift location.
7. Players are teleported to predefined starting positions around that Rift.
8. Teams fight over the Rift.
9. Rift completion is detected.
10. Players return to the staging/draft area.
11. Another Rift can begin.
12. Eventually this becomes a complete match/session system such as Bo3,
    Bo5, Bo7, or continuous play.
Bots were originally considered for representing draft heroes but were
abandoned for now.
---
# 3. FAKE DRAFT
Current hardcoded draft:
```csharp
private static readonly Heroes[] SapphireDraft =
[
    Heroes.Shiv,
    Heroes.Yamato,
    Heroes.Mirage,
    Heroes.Wraith,
    Heroes.Krill,
    Heroes.Viper
];
private static readonly Heroes[] AmberDraft =
[
    Heroes.Fencer,
    Heroes.Drifter,
    Heroes.Doorman,
    Heroes.Werewolf,
    Heroes.PunkGoat,
    Heroes.Magician
];

Current team mapping:

* SapphireDraft -> team 3
* AmberDraft -> team 2

Players use /select <hero_name>.

Current behavior/concepts:

* One hero per player.
* No duplicate selections.
* /unselect returns player to Skyrunner.
* Unselected players can remain/spectate above the draft area.

⸻

4. VERIFIED HERO ENUM

public enum Heroes
{
  Inferno = 1,
  Gigawatt = 2,
  Hornet = 3,
  Ghost = 4,
  Atlas = 6,
  Wraith = 7,
  Forge = 8,
  Chrono = 10,
  Dynamo = 11,
  Kelvin = 12,
  Haze = 13,
  Astro = 14,
  Bebop = 15,
  Nano = 16,
  Orion = 17,
  Krill = 18,
  Shiv = 19,
  Tengu = 20,
  Kali = 21,
  Warden = 25,
  Yamato = 27,
  Lash = 31,
  Viscous = 35,
  Gunslinger = 38,
  Yakuza = 39,
  Tokamak = 47,
  Wrecker = 48,
  Rutger = 49,
  Synth = 50,
  Thumper = 51,
  Mirage = 52,
  Slork = 53,
  Cadence = 54,
  Bomber = 56,
  ShieldGuy = 57,
  Viper = 58,
  Vandal = 59,
  Magician = 60,
  Trapper = 61,
  Operative = 62,
  VampireBat = 63,
  Drifter = 64,
  Priest = 65,
  Frank = 66,
  Bookworm = 67,
  Boho = 68,
  Doorman = 69,
  Skyrunner = 70,
  Swan = 71,
  PunkGoat = 72,
  Druid = 73,
  Graf = 74,
  Fortuna = 75,
  Necro = 76,
  Fencer = 77,
  Airheart = 78,
  Familiar = 79,
  Werewolf = 80,
  Unicorn = 81,
  Opera = 82
}

⸻

5. DRAFT AREA

private static readonly Vector3 DraftPosition =
    new(0f, 0, 1536.062500f);
private static readonly Vector3 DraftAngle =
    new(0.000000f, 90.687500f, 0.000000f);

Known-good teleport:

private void MoveToDraftPosition(CCitadelPlayerController player)
{
    var pawn = player.GetHeroPawn();
    if (pawn == null) return;
    pawn.Teleport(
        position: DraftPosition,
        angles: null,
        velocity: Vector3.Zero
    );
    SetPlayerAngle(player, DraftAngle);
}

Camera helper:

private static void SetPlayerAngle(
    CCitadelPlayerController player,
    Vector3 angle)
{
    NetMessages.Send(
        new CCitadelUserMsg_SetClientCameraAngles
        {
            PlayerSlot = player.Slot,
            CameraAngles = new CMsgQAngle
            {
                X = angle.X,
                Y = angle.Y,
                Z = angle.Z
            }
        },
        RecipientFilter.Single(player.Slot)
    );
}

Moving selected players belonging to a draft/team:

private void MoveActivePlayers(
    Heroes[] teamHeroes,
    Vector3 position,
    Vector3 angle)
{
    foreach (var player in Players.GetAll())
    {
        if (!PlayerSelections.TryGetValue(
            player.PlayerSteamId,
            out var hero))
            continue;
        if (!teamHeroes.Contains(hero))
            continue;
        var pawn = player.GetHeroPawn();
        if (pawn == null) continue;
        pawn.Teleport(
            position: position,
            angles: null,
            velocity: Vector3.Zero
        );
        SetPlayerAngle(player, angle);
    }
}

⸻

6. WORLD TEXT

CPointWorldText / point_worldtext works.

Known cleanup:

foreach (var entity in Entities.ByDesignerName("point_worldtext"))
{
    entity.Remove();
}

Important Deadworks API detail:

Entities.All is an enumerable PROPERTY.

Correct:

foreach (var entity in Entities.All)

Incorrect:

Entities.All()
Entities.All<CBaseEntity>()

Players.GetAll() IS a method.

⸻

7. HERO CHANGE SAFETY

This is important.

Repeated native crashes showed that:

CCitadelPlayerController.SelectHero()

is unsafe during the player’s death/death lifecycle.

Current guard:

private static bool CanChangeHero(CCitadelPlayerController player)
{
    var pawn = player.GetHeroPawn();
    return pawn != null && pawn.IsAlive;
}

Do not call SelectHero() while the player is dead.

⸻

8. RESPAWNS / DEATH TIMERS

Experiments showed:

citadel_player_override_spawn_time 600

creates a 600-second respawn timer.

Changing the value while somebody is already dead does NOT shorten their
existing timer.

Current simpler intended approach:

* use a very short/normal respawn time
* allow the game to respawn the player normally
* intercept player_spawn
* teleport them back to the Rift Rumble draft/staging position

Teleporting a dead pawn itself is insufficient because the later normal
respawn still places the player at the game’s team spawn.

There is not yet a finalized eliminated-player system.

⸻

9. RIFT / KOTH IMPLEMENTATION

Rift spawning through GameRules is working.

GameRules pointer:

var gameRules = gameRulesProxy.GetField<nint>(
    "CCitadelGameRulesProxy"u8,
    "m_pGameRules"u8
);

Known fields:

m_timeNextKothSpawn
m_timeNextKothSpawnWindowTime
m_vNextKothLocation

These are accessed using SchemaAccessor<float> /
SchemaAccessor<Vector3>.

⸻

10. REAL RIFT LOCATIONS

Green:

private static readonly Vector3 GreenRiftPosition =
    new(7612f, -0.000661f, 444f);

Yellow:

private static readonly Vector3 YellowRiftPosition =
    new(-7560f, 0f, 424f);

Middle <0,0,0> is intentionally excluded.

Alternation currently uses:

private bool _nextRiftIsGreen = true;

⸻

11. RIFT PLAYER START POSITIONS

Green Sapphire:

8225.000000 1797.718750 248.062500
angle: 0 -100.343750 0

Green Amber:

7025.656250 -2088.593750 256.031250
angle: 0 80.718750 0

Yellow Sapphire:

-8259.562500 -2134.312500 248.031250
angle: 0 80.875000 0

Yellow Amber:

-7072.656250 2209.531250 248.031250
angle: 0 -102.156250 0

⸻

12. KNOWN-GOOD RIFT SPAWN SEQUENCE

The currently working approach is:

1. Find GameRules.
2. Snapshot existing citadel_item_koth_spawner entities.
3. Snapshot existing npc_trooper entities.
4. Disable KOTH.
5. Set desired Rift location.
6. Set next KOTH window/spawn to 0.
7. Enable KOTH.
8. Wait for a NEW citadel_item_koth_spawner.
9. Once the Rift exists, park the scheduler.
10. Disable automatic KOTH again.
11. Teleport players into Rift starting positions.
12. Watch for completion.

Parking scheduler:

nextWindow.Set(gameRules, 999999f);
nextSpawn.Set(gameRules, 999999f);
ConVar.Find("citadel_koth_enabled")?.SetInt(0);

DO NOT casually alter this sequence. Previous attempts to add too much
diagnostic/gameplay logic broke Rift spawning/teleports/scheduler behavior.

Snapshot:

var existingSpawners = Entities
    .ByDesignerName("citadel_item_koth_spawner")
    .Select(entity => entity.EntityIndex)
    .ToHashSet();

Detect new spawner:

var spawner = Entities
    .ByDesignerName("citadel_item_koth_spawner")
    .FirstOrDefault(entity =>
        !existingSpawners.Contains(entity.EntityIndex)
    );

⸻

13. RIFT CAPTURE COMPLETION SIGNAL

A successful Rift capture causes NEW:

npc_trooper

entities to appear.

Snapshot before Rift:

var troopersBeforeRift = Entities
    .ByDesignerName("npc_trooper")
    .Select(entity => entity.EntityIndex)
    .ToHashSet();

Completion watcher finds a trooper not in that snapshot.

After completion, remove only the newly created troopers.

IMPORTANT:

Do NOT remove:

info_super_trooper_spawn

Previous testing caused a crash when doing that.

⸻

14. RIFT TIE / GIVE-UP SIGNAL

Entity snapshots were taken around successful captures and tied Rifts.

Normal capture:

* new npc_trooper entities appear

Tie/give-up:

citadel_koth_cashin

disappears WITHOUT new npc_trooper entities appearing.

Server reports:

Comeback Urn Give Up: 60.02s Elapsed time

Verified tie detection:

var sawKothCashin = false;

During watcher:

if (newTrooper == null)
{
    var cashinExists = Entities
        .ByDesignerName("citadel_koth_cashin")
        .Any();
    if (cashinExists)
    {
        sawKothCashin = true;
    }
    else if (sawKothCashin)
    {
        Console.WriteLine(
            "[RiftRumble] RIFT TIED | KOTH cash-in disappeared."
        );
        return riftStep.Done();
    }
    return riftStep.Wait(1.Ticks());
}

This has been experimentally verified.

The tie branch currently duplicates the same three-second return/cleanup
behavior as successful capture. This duplication was intentionally accepted
temporarily rather than refactoring while debugging.

This duplication is now a good candidate for the upcoming refactor.

⸻

15. ENTITY SNAPSHOT DEVTOOLS

A separate DevTools plugin/command system has been useful for discovering
Deadlock behavior.

Important discovery:

EntityIndex is NOT globally unique across everything returned by
Entities.All.

An earlier dictionary keyed by EntityIndex failed with duplicate key 0.

Working snapshot representation:

private List<(int Index, string DesignerName, string Classname, string Name)>?
    _entitySnapshot;

Snapshot:

[Command("snapshot")]
public void SnapshotEntities()
{
    _entitySnapshot = Entities.All
        .Select(entity => (
            Index: entity.EntityIndex,
            DesignerName: entity.DesignerName,
            Classname: entity.Classname,
            Name: entity.Name
        ))
        .ToList();
    Console.WriteLine(
        $"[DevTools] Entity snapshot taken | " +
        $"Count={_entitySnapshot.Count}"
    );
}

Compare performs tuple comparison against another Entities.All list and
prints ADDED/REMOVED entities.

DevTools should remain useful for experimentation rather than stuffing
temporary diagnostics into Rift Rumble itself.

⸻

16. CURRENT KOTH GIVE-UP INVESTIGATION

Current tied Rift automatically gives up after approximately:

60.02 seconds

Tested:

citadel_koth_spawn_window 120

This does NOT change the 60-second give-up time.

Known KOTH convars include:

citadel_koth_dev_test_spawn
citadel_koth_early_warning_time
citadel_koth_enabled
citadel_koth_respawn_interval
citadel_koth_reward_base
citadel_koth_reward_buff_count
citadel_koth_reward_time_multiplier
citadel_koth_spawn_initial_delay
citadel_koth_spawn_location_deck_count
citadel_koth_spawn_window
citadel_koth_warning_time

None has yet been verified as controlling the tie timeout.

⸻

17. m_timeKothGiveUp INVESTIGATION

CCitadelGameRules contains:

m_timeKothCashInStarted
m_timeKothGiveUp

Testing m_timeKothGiveUp showed:

Immediately around Rift spawning:

KOTH GiveUp=0

Once citadel_koth_cashin appeared:

GiveUp=3.4028235E+38

That is effectively float.MaxValue.

Despite that value, the Rift still gives up at 60.02 seconds.

Conclusion:

m_timeKothGiveUp does NOT appear to directly contain the 60-second deadline
we are trying to configure.

Do not continue blindly writing this field.

⸻

18. KOTH CASH-IN VDATA DISCOVERY

The runtime:

CCitadel_KothCashIn

contains state such as:

m_bGiveUpHasWarned
m_bGivenUp
m_bWasBlockedAtAnyPoint
m_bCashedIn
m_bPlayBlock
m_bPlayContested

The VData hierarchy is:

CEntitySubclassVDataBase
└── CCitadel_MultiCapturePointVData
    └── CCitadel_KothCashInVData

The game’s data/resource representation contains:

TimeToWarnAboutGivingUp = 50
TimeToGiveUp = 60
GiveUpOrbs = 13

and capture timings including:

TotalTimeToCapture = 12
TotalTimeToCaptureFavored = 6
TotalTimeToCaptureUnfavored = 18

The exact 60 value therefore exists in the Rift/KOTH VData/resource.

However, current public schema inspection did NOT reveal an ordinary
schema-reflected field named TimeToGiveUp / m_flTimeToGiveUp.

Do NOT invent a SchemaAccessor field name.

⸻

19. SubclassVData POINTER IS VERIFIED

Deadworks exposes:

cashin.SubclassVData

Testing against the actual citadel_koth_cashin produced:

KOTH CashIn VData |
Name=citadel_koth_cashin |
Handle=4490956523520

The handle changes/should be treated as runtime data; do not hardcode that
specific numeric pointer.

This confirms the runtime cash-in entity has valid SubclassVData.

Example:

var cashin = Entities
    .ByDesignerName("citadel_koth_cashin")
    .FirstOrDefault();
if (cashin?.SubclassVData != null)
{
    var vdata = cashin.SubclassVData.Handle;
    Console.WriteLine(
        $"[RiftRumble] CashIn VData pointer={vdata}"
    );
}

The unresolved problem is how to access/change the KV/resource value:

TimeToGiveUp = 60

because it does not appear to be exposed as an ordinary reflected schema
member.

This investigation can continue later. It is NOT the immediate priority of
the refactor discussion unless useful.

⸻

20. STARTUP / CURRENT CONVARS

Known startup behavior includes:

public override void OnStartupServer()
{
    ConVar.Find("citadel_team_size")?.SetInt(6);
    ConVar.Find("maxplayers")?.SetInt(12);
    ConVar.Find("sv_visiblemaxplayers")?.SetInt(12);
    ConVar.Find("citadel_koth_enabled")?.SetInt(0);
    Server.ExecuteCommand("citadel_koth_warning_time 1");
    Server.ExecuteCommand("citadel_koth_early_warning_time 1");
    ConVar.Find("citadel_allow_purchasing_anywhere")?.SetInt(1);
    ConVar.Find("citadel_allow_duplicate_heroes")?.SetInt(1);
    Timer.NextTick(CreateBoards);
}

Some old logs/code still say:

[Rift Wars]

Project name is now:

Rift Rumble

New/refactored logging should use:

[RiftRumble]

⸻

21. OTHER VERIFIED DEADWORKS PATTERNS

Server command execution:

Server.ExecuteCommand("...");

Plugin command:

[Command("testing")]
public void Testing()

is invoked from server console as:

dw_testing

Commands taking:

CCitadelPlayerController caller

require player context/chat and cannot simply be invoked as equivalent
server-console commands.

Known event/API details:

PlayerDeathEvent.UseridPawn

is CBasePlayerPawn and does not expose HeroID.

Client connection override:

OnClientFullConnect(ClientFullConnectEvent args)

Hero changed event can use:

[GameEventHandler("player_hero_changed")]

returning HookResult.

⸻

22. CURRENT DIRECTION / NEXT DISCUSSION: REFACTOR

THIS IS THE MAIN REASON FOR STARTING THE NEW CHAT.

The existing plugin has accumulated working experiments and game logic in a
way that is becoming difficult to reason about.

Before continuing substantial gameplay work, I want to design a refactor
strategy.

Do NOT immediately generate a huge new architecture or rewrite everything.

First help me reason through how Rift Rumble should be decomposed, what the
boundaries should be, and how to perform the refactor safely.

After we agree on the architecture, I want to figure out how to communicate
the plan clearly to Cursor so Cursor can perform much of the mechanical
refactoring without changing working behavior.

⸻

23. REFACTOR IDEA: COMMANDS AS SMALL UNITS

One architecture idea I want to explore:

Treat useful operations as small, individually callable commands/actions.

For example, conceptually:

Spawn Rift
Move Sapphire to Rift
Move Amber to Rift
Return Players to Draft
Start Draft
Select Hero
Unselect Hero
Cleanup Rift
Start Rift Round
End Rift Round
etc.

The game flow can then COMPOSE those lower-level operations.

Conceptually:

Game Flow
    ↓
Start Rift Round
    ↓
Spawn Rift
    ↓
Move Teams
    ↓
Watch Rift
    ↓
Resolve Capture/Tie
    ↓
Cleanup Rift
    ↓
Return Players

This could make each operation:

* easier to test
* easier to debug
* easier to invoke manually
* easier to log
* easier to reuse
* less coupled to the entire match lifecycle

However, do not assume every internal function literally needs to be a
Deadworks [Command]. Discuss whether there should be a distinction between:

1. core operation/service
2. admin command wrapper
3. game-flow orchestration

For example, SpawnRift() may be the actual operation while dw_koth or
another admin command merely calls it.

I want to reason through this design rather than blindly implementing it.

⸻

24. ADMIN-ONLY DEBUG / CONTROL COMMANDS

Commands that normal players do NOT need should be restricted.

Examples might include:

/koth
force Rift
force cleanup
force next round
teleport teams
reset match
diagnostic commands

For now, admin authorization can simply be based on MY Steam ID.

There is existing DevTools work/code that exposes my Steam ID / can be used to
identify it.

Do NOT invent the Steam ID. Inspect the current source/DevTools code when we
get to implementation.

Longer term this could become a more formal admin system, but that is not
necessary now.

Normal player commands such as:

/select
/unselect

should remain accessible to players according to game-state rules.

We should discuss a clean authorization boundary rather than scattering Steam
ID checks throughout every command.

⸻

25. DESIRED FILE / COMPONENT ORGANIZATION

I want to break the current plugin into smaller components across files.

The architecture should make it immediately understandable where things live.

Potential conceptual areas include, but are NOT predetermined:

Commands/
GameFlow/
Rifts/
Draft/
Players/
Teams/
Configuration/
Logging/
Admin/
Events/

These names are only examples.

Do not create needless enterprise-style abstractions.

This is a game-mode plugin, so the structure should remain practical and
relatively small.

We should inspect the current source before finalizing the actual folder/file
structure.

Goals:

* small focused files
* clear responsibilities
* minimal duplicated logic
* game flow separated from low-level operations
* admin/debug interfaces separated from actual game logic
* reusable operations
* easy manual testing
* easy future feature development
* understandable enough that Cursor can refactor safely

⸻

26. LOGGING DESIGN GOAL

I want COMPREHENSIVE logging, but not one enormous unreadable log.

Desired idea:

Master Log

A high-level Rift Rumble log containing:

* startup/shutdown
* important state transitions
* rounds starting/ending
* serious warnings
* errors
* failures
* unexpected conditions

The master log should let me understand:

“What happened to the game?”

When a problem belongs to a particular feature/system, the master log should
make it obvious which subsystem should be investigated.

Conceptually:

[RiftRumble] ERROR | Rift round failed to initialize | See Rift log

Feature Logs

Individual systems should have their own detailed logs.

Examples might include:

rift.log
draft.log
players.log
gameflow.log
commands.log

Exact files/categories need to be designed.

A Rift-specific log could contain detailed information such as:

* requested Rift side
* GameRules values
* spawner discovery
* KOTH entity lifecycle
* cash-in appearance
* trooper completion
* tie detection
* cleanup
* timing

The master log should NOT need every low-level diagnostic detail.

Feature logs answer:

“What exactly happened inside this subsystem?”

We need to determine what Deadworks/.NET logging/file APIs are appropriate
before implementing this. Do not assume arbitrary file access/logging
capabilities without checking.

Also consider:

* consistent timestamping
* severity levels
* subsystem/category
* round/session identifiers
* player Steam ID/slot when relevant
* exception details
* avoiding excessive per-tick spam
* keeping logs useful during actual debugging

⸻

27. GAME FLOW / STATE MANAGEMENT

The refactor should likely introduce a clearer concept of game state.

Current functionality grew experimentally, so behavior is often driven
directly from commands/timers.

We should discuss whether Rift Rumble should explicitly represent states such
as:

Waiting
Draft
PreparingRift
RiftActive
ResolvingRift
BetweenRounds
MatchFinished

Exact states are not decided.

The important design question is:

Commands/actions should perform operations, while some central game-flow
component decides WHEN those operations happen.

This would prevent individual commands from becoming the game mode itself.

Admin commands could then trigger the same operations manually for testing.

⸻

28. IMPORTANT REFACTOR PRINCIPLE: PRESERVE BEHAVIOR

The existing plugin contains ugly/duplicated code that WORKS.

Do not clean up everything simultaneously.

Recommended refactor discussion should include a staged strategy such as:

1. inventory current behavior
2. identify stable boundaries
3. add/standardize logging
4. extract constants/configuration
5. extract low-level operations without changing behavior
6. wrap operations with admin/debug commands
7. extract game-flow orchestration
8. introduce explicit state management if appropriate
9. consolidate duplicated round resolution/cleanup
10. only then continue new gameplay features

The exact plan should be discussed and improved before implementation.

The goal is to avoid repeating the earlier incident where adding diagnostic
logic to the working KOTH method accidentally broke:

* player teleporting
* scheduler parking
* trooper cleanup

Known-good behavior is more important than immediately making the code pretty.

⸻

29. CURSOR HANDOFF GOAL

After designing the architecture, I want to use Cursor to perform the
refactoring.

We should eventually create a clear implementation/refactor specification for
Cursor.

It should explain:

* current architecture
* desired architecture
* exact responsibilities of components
* files to create/move
* dependencies between systems
* public interfaces
* admin authorization
* logging strategy
* game-state ownership
* which existing behavior MUST remain unchanged
* known Deadworks API constraints
* dangerous areas not to modify casually
* sequence in which refactor steps should occur
* tests/manual verification after each stage

Cursor should NOT be told simply:

“Refactor this plugin.”

We should produce a staged set of instructions where each stage can be
reviewed/tested before moving to the next.

Ideally Cursor performs mechanical extraction/reorganization while we retain
control over architectural decisions.

⸻

30. FIRST TASK IN THE NEW CHAT

Start by discussing the architecture/refactor strategy with me.

Specifically, evaluate the idea of:

* operations as small reusable units
* admin commands as wrappers around those operations
* game flow composing operations
* centralized game state
* admin authorization based initially on my Steam ID
* master + subsystem logging
* splitting the current plugin into focused files/components

Do NOT immediately write the complete refactored plugin.

First propose a practical architecture and explain how the pieces would
interact.

Keep it appropriate for the actual size of Rift Rumble rather than designing
a huge enterprise application.

After we agree on the architecture, we will inspect the current source and
create a staged Cursor refactor plan.

This version deliberately makes the next chat about architecture first, with the current Rift/KOTH research preserved as implementation context rather than letting it dominate the discussion.
