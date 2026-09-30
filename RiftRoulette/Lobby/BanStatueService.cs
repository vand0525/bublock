using Bublock.Modules.Hud;
using Bublock.Modules.Loadout;
using Bublock.Modules.Restraint;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Draft;
using RiftRoulette.Duel;
using RiftRoulette.GameLoop;
using RiftRoulette.RandomMode;
using RiftRoulette.Round;
using RiftRoulette.Stats;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.Lobby;

public static class BanStatueService
{
  public const int LiveBanKickSeconds = 30;

  public const int RejoinKickSeconds = 10;

  public const int SustainSeconds = 1;

  private const float ModifierSeconds = LiveBanKickSeconds + 5;

  public const string BannedMessage = "You are banned. Do better.";

  public static string BannedTitle(string name) => $"{name} is banned";

  public static readonly IReadOnlyList<string> StoneJokes =
  [
    "Turned to stone. Great for the decor.",
    "Rock solid decision.",
    "Stone cold. Literally.",
    "Now a statue. Pigeons welcome.",
    "Taking some time to think about it... as a rock."
  ];

  // modifier_citadel_petrify renders as a red wireframe unless Vyper's assets are loaded.
  public const Heroes StatueLookHero = Heroes.Viper;

  private static readonly Logger Log = BublockLog.For("Access");

  private static readonly Dictionary<ulong, BanRecord> Records = [];

  private static readonly HashSet<ulong> Statues = [];

  private static readonly HashSet<ulong> Arriving = [];

  private static readonly HashSet<ulong> ModifierWarned = [];

  public static bool IsStatue(ulong steamId) => Statues.Contains(steamId);

  public static int Count => Statues.Count;

  // Runs in OnClientConnect for a banned Steam ID. True lets them in as a statue.
  public static bool AdmitBanned(ulong steamId, string name)
  {
    var now = DateTime.UtcNow;
    var record = Records.GetValueOrDefault(steamId, BanRecord.None);

    if (BanJoinRule.Decide(record, now) == BanJoinDecision.Refuse)
    {
      Log.Warn("Connection refused, banned and locked out Name={Name} SteamId={SteamId} {Record}", name, steamId, BanJoinRule.Describe(record, now));
      return false;
    }

    record = BanJoinRule.Strike(record, now);
    Records[steamId] = record;
    Arriving.Add(steamId);

    Log.Warn("Banned player let in as a statue Name={Name} SteamId={SteamId} {Record}", name, steamId, BanJoinRule.Describe(record, now));
    return true;
  }

  public static bool TakeArrival(ulong steamId) => Arriving.Remove(steamId);

  public static void Petrify(
    CCitadelPlayerController player,
    int kickAfterSeconds,
    bool liveBan,
    ITimer timer,
    ExecutionMode mode = ExecutionMode.Clean)
  {
    var steamId = player.PlayerSteamId;
    var log = Log.WithMode(mode);

    if (!Statues.Add(steamId))
      return;

    TakeOutOfGame(player, timer, mode);

    if (!liveBan)
    {
      var team = TeamBalance.SmallerTeam(Participants.Humans().Select(other => other.TeamNum), Random.Shared);
      player.SelectHero(LobbyService.LobbyHero);
      player.ChangeTeam(team, true);
    }

    WatchSpot.SendUp(player, mode);

    var own = liveBan ? $"{BannedMessage} You will be kicked in {kickAfterSeconds}s." : BannedMessage;

    timer.Once(LoadoutService.SwapDelaySeconds.Seconds(), () =>
    {
      if (Find(steamId) is not { } current)
        return;

      Hold(current);
      PlayerChat.Send(current, own);
    });

    HudService.AnnounceAll(BannedTitle(player.PlayerName), StoneJokes[Random.Shared.Next(StoneJokes.Count)], mode);
    StreamCam.ShowStatue(steamId);

    log.Info(player.ToPlayerRef(), "Banned player turned to stone KickAfter={KickAfter} LiveBan={LiveBan}", kickAfterSeconds, liveBan);
    BublockLog.Master.Info(player.ToPlayerRef(), "Banned player turned to stone KickAfter={KickAfter}", kickAfterSeconds);

    timer.Once(kickAfterSeconds.Seconds(), () =>
    {
      if (Find(steamId) is { } current)
        LobbyService.KickPlayer(current.Slot, mode);
    });
  }

  public static void Sustain()
  {
    if (Statues.Count == 0)
      return;

    foreach (var player in Players.GetAll().Where(player => Statues.Contains(player.PlayerSteamId)))
    {
      RestraintService.Restrain(player);
      Hold(player);
    }
  }

  // A disconnect ends the statue; strikes stay until restart.
  public static void Forget(ulong steamId)
  {
    Statues.Remove(steamId);
    Arriving.Remove(steamId);
    ModifierWarned.Remove(steamId);
  }

  public static int KickConnectedBanned(ExecutionMode mode = ExecutionMode.Clean)
  {
    var kicked = 0;

    foreach (var player in Players.GetAll().Where(player => !player.IsBot && AccessService.Check(player) == AccessVerdict.Banned).ToList())
    {
      if (LobbyService.KickPlayer(player.Slot, mode))
        kicked++;
    }

    if (kicked > 0)
      Log.WithMode(mode).Info("Banned players kicked after reload Kicked={Kicked}", kicked);

    return kicked;
  }

  public static string Describe(ulong steamId)
  {
    var record = Records.GetValueOrDefault(steamId, BanRecord.None);
    var statue = Statues.Contains(steamId) ? " statue now" : "";
    return $"{BanJoinRule.Describe(record, DateTime.UtcNow)}{statue}";
  }

  private static void TakeOutOfGame(CCitadelPlayerController player, ITimer timer, ExecutionMode mode)
  {
    var steamId = player.PlayerSteamId;
    DuelService.Forget(steamId);

    if (DraftState.Release(steamId, out _))
      DraftService.RedrawBoards(mode);

    if (MatchService.State.IsRunning && MatchConfig.IsRandom)
      RandomModeService.OnLeave(steamId, timer, mode);

    StatsService.RefreshBoards(mode);
    AutoStartService.Check(timer, mode, steamId);
  }

  private static void Hold(CCitadelPlayerController player)
  {
    var pawn = player.GetHeroPawn();

    if (pawn == null || !pawn.IsAlive || pawn.ModifierProp is not { } states)
      return;

    var modifier = AccessService.Load().StatueModifier;

    if (string.IsNullOrWhiteSpace(modifier))
    {
      if (ModifierWarned.Add(player.PlayerSteamId))
        Log.Warn(player.ToPlayerRef(), "Statue modifier not set, restraint only - set it with /ban_modifier");

      return;
    }

    if (states.HasModifier(modifier))
      return;

    if (!RestraintService.AddModifier(pawn, modifier, ModifierSeconds) && ModifierWarned.Add(player.PlayerSteamId))
      Log.Warn(player.ToPlayerRef(), "Statue modifier refused, restraint only Modifier={Modifier}", modifier);
  }

  private static CCitadelPlayerController? Find(ulong steamId) =>
    Players.GetAll().FirstOrDefault(player => player.PlayerSteamId == steamId);
}
