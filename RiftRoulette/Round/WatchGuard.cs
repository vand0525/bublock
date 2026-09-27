using Bublock.Modules.Restraint;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Lobby;
using RiftRoulette.Rift;

namespace RiftRoulette.Round;

public static class WatchGuard
{
  public const int CheckEveryFrames = 16;

  public const double GraceSeconds = 2.0;

  public const double CommandRepeatSeconds = 2.0;

  private static readonly Logger Log = BublockLog.For("Watch");

  private static readonly Dictionary<ulong, DateTime> GraceUntil = [];

  private static readonly Dictionary<(ulong, string), DateTime> LastCommand = [];

  private static int _frame;

  public static int Rescues { get; private set; }

  public static void Grace(ulong steamId) =>
    GraceUntil[steamId] = DateTime.UtcNow.AddSeconds(GraceSeconds);

  public static void Forget(ulong steamId)
  {
    GraceUntil.Remove(steamId);

    foreach (var key in LastCommand.Keys.Where(key => key.Item1 == steamId).ToList())
      LastCommand.Remove(key);
  }

  public static int Tick(ExecutionMode mode = ExecutionMode.Clean) =>
    ++_frame % CheckEveryFrames == 0 ? Check(mode) : 0;

  public static int Check(ExecutionMode mode = ExecutionMode.Clean)
  {
    if (RestraintService.Count == 0)
      return 0;

    var side = WatchSpot.Side;
    var spotZ = WatchSpot.Location(side).Position.Z;
    var now = DateTime.UtcNow;
    var rescued = 0;

    foreach (var player in Participants.Humans())
    {
      var steamId = player.PlayerSteamId;

      if (!RestraintService.IsRestrained(steamId))
        continue;

      if (GraceUntil.TryGetValue(steamId, out var until) && now < until)
        continue;

      var pawn = player.GetHeroPawn();

      if (pawn == null || !pawn.IsAlive)
        continue;

      var z = pawn.Position.Z;

      if (!WatchGuardRule.IsBelow(z, spotZ))
        continue;

      WatchSpot.SendUp(player, mode, side);
      Rescues++;
      rescued++;

      Log.WithMode(mode).Info(
        player.ToPlayerRef(),
        "Rescued below the watch spot Z={Z} Line={Line} Side={Side} Rescues={Rescues}",
        z,
        WatchGuardRule.Line(spotZ),
        RiftSides.Name(side),
        Rescues);
    }

    return rescued;
  }

  public static void LogCommand(CCitadelPlayerController player, string command, string[] args)
  {
    var key = (player.PlayerSteamId, command);
    var now = DateTime.UtcNow;

    if (LastCommand.TryGetValue(key, out var last) && (now - last).TotalSeconds < CommandRepeatSeconds)
      return;

    LastCommand[key] = now;
    Log.Info(player.ToPlayerRef(), "Restrained player console command Command={Command} Args={Args}", command, string.Join(' ', args));
  }
}
