using System.Numerics;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.GameLoop;
using RiftRoulette.Lobby;
using RiftRoulette.RandomMode;
using RiftRoulette.Rift;

namespace RiftRoulette.HeroAssist;

public static class RemCreepsService
{
  public const string TrooperName = "npc_trooper";

  public const Heroes RemHero = Heroes.Familiar;

  private const float CreepSpacing = 80f;

  private static readonly Logger Log = BublockLog.For("HeroAssist");

  private static readonly List<int> Spawned = [];

  public static int Count { get; private set; } = RemCreepsRule.DefaultCount;

  public static int SpawnedCount => Spawned.Count;

  public static string SetCount(int count, ExecutionMode mode = ExecutionMode.Clean)
  {
    Count = RemCreepsRule.ClampCount(count);
    Log.WithMode(mode).Info("Rem creep count set Count={Count}", Count);
    return Count == 0 ? "Rem creeps off (0)." : $"Rem creeps set to {Count}.";
  }

  public static int TrySpawn(RiftSide side, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    ClearIndexes();

    var remIsFighter = RandomModeService.FighterCount > 0 &&
      Participants.Humans().Any(player =>
        RandomModeService.TryGetAssignment(player.PlayerSteamId, out var assignment) &&
        assignment.Hero == RemHero);

    if (!RemCreepsRule.ShouldSpawn(MatchConfig.IsRandom, RandomModeService.FighterCount, remIsFighter, Count))
    {
      log.Debug(
        "Rem creeps skipped Random={Random} Fighters={Fighters} Rem={Rem} Count={Count}",
        MatchConfig.IsRandom,
        RandomModeService.FighterCount,
        remIsFighter,
        Count);
      return 0;
    }

    var rem = Participants.Humans().FirstOrDefault(player =>
      RandomModeService.TryGetAssignment(player.PlayerSteamId, out var assignment) &&
      assignment.Hero == RemHero);

    if (rem == null)
    {
      log.Warn("Rem creeps refused, Rem fighter not connected");
      return 0;
    }

    var team = rem.TeamNum;
    var origin = rem.GetHeroPawn()?.Position ?? RiftSides.Position(side);
    var spawned = 0;

    for (var i = 0; i < Count; i++)
    {
      var entity = CBaseEntity.CreateByDesignerName(TrooperName);

      if (entity == null)
      {
        log.Warn(rem.ToPlayerRef(), "Rem creep create refused Index={Index}", i);
        continue;
      }

      entity.TeamNum = team;
      entity.Spawn();

      var offset = new Vector3((i - (Count - 1) / 2f) * CreepSpacing, 0f, 0f);
      entity.Teleport(origin + offset, angles: null, velocity: Vector3.Zero);
      Spawned.Add(entity.EntityIndex);
      spawned++;
    }

    if (spawned > 0)
      RiftService.NoteTroopers(Spawned);

    log.Info(
      rem.ToPlayerRef(),
      "Rem creeps spawned Count={Count} Team={Team} Side={Side}",
      spawned,
      RiftRouletteTeams.Name(team),
      RiftSides.Name(side));

    return spawned;
  }

  public static void Clear(ExecutionMode mode = ExecutionMode.Clean)
  {
    if (Spawned.Count == 0)
      return;

    Log.WithMode(mode).Debug("Rem creep indexes cleared Count={Count}", Spawned.Count);
    ClearIndexes();
  }

  public static string Describe() =>
    $"RemCreeps={Count} (0=off) | Spawned={Spawned.Count} | Default={RemCreepsRule.DefaultCount}";

  private static void ClearIndexes() => Spawned.Clear();
}
