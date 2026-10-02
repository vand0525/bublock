using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Mirror;
using RiftRoulette.RandomMode;
using RiftRoulette.Round;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.Lobby;

public static class LobbyHeroes
{
  private static readonly Logger Log = BublockLog.For("Lobby");

  public static int ReturnAll(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var log = Log.WithMode(mode);
    var returned = 0;

    RoundHeroes.Clear();

    foreach (var player in Participants.Humans())
    {
      var pawn = player.GetHeroPawn();

      if (pawn == null)
        continue;

      if (!pawn.IsAlive)
      {
        log.Debug(player.ToPlayerRef(), "Lobby return skipped dead player LifeState={LifeState}", pawn.LifeState);
        continue;
      }

      ClearProgression(pawn);
      player.SelectHero(LobbyService.LobbyHero);

      timer.NextTick(() => WatchSpot.SendUp(player, mode));
      returned++;
    }

    log.Info("Players returned to the lobby hero Returned={Returned}", returned);
    return returned;
  }

  public static void Enforce(CCitadelPlayerController player, CCitadelPlayerPawn pawn, ITimer timer)
  {
    if (!Participants.IsParticipant(player))
      return;

    // #region agent log
    if (Bublock.Modules.Loadout.LoadoutStress.Running)
      return;
    // #endregion

    if (RandomModeService.GuardHero(player, pawn, timer))
      return;

    if (MirrorModeService.GuardHero(player, pawn, timer))
      return;

    var hasRoundHero = RoundHeroes.TryGet(player.PlayerSteamId, out var roundHero);
    var expectedHero = hasRoundHero ? roundHero : LobbyService.LobbyHero;

    if (pawn.HeroID != expectedHero)
    {
      if (!pawn.IsAlive)
      {
        Log.Debug(
          player.ToPlayerRef(),
          "Hero enforcement skipped while dead Current={Current} Expected={Expected} LifeState={LifeState}",
          pawn.HeroID,
          expectedHero,
          pawn.LifeState);

        return;
      }

      player.SelectHero(expectedHero);
      return;
    }

    if (!hasRoundHero)
      ClearProgression(pawn);
  }

  private static void ClearProgression(CCitadelPlayerPawn pawn)
  {
    pawn.SetCurrency(ECurrencyType.EGold, 0);
    pawn.SetCurrency(ECurrencyType.EAbilityPoints, 0);
    pawn.Level = 0;
  }
}
