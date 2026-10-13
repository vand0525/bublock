using Bublock.Modules.Loadout;
using Bublock.Modules.MidBoss;
using Bublock.Shared;
using DeadworksManaged.Api;

namespace CrystalCrackers.Pit;

public class PitRoundPlugin : DeadworksPluginBase
{
  private static readonly Logger PitLog = BublockLog.For("Pit");

  public override string Name => "Crystal Crackers Pit Round";

  // The watch spots stand on the sky clip floor (z 1536); on, the game drops players off it.
  public const string SkyClipConVar = "citadel_check_walk_on_skyclip";

  public override void OnLoad(bool isReload)
  {
    ServerConVars.TrySet(SkyClipConVar, 0, PitLog);
    PitLocations.RegisterAll();
    MidBossService.BossKilled += PitRoundService.OnBossKilled;
    MidBossService.CrystalTaken += PitRoundService.OnCrystalTaken;
    Timer.Every(1.Seconds(), PitRoundService.WatchCredit);
  }

  public override void OnUnload()
  {
    MidBossService.BossKilled -= PitRoundService.OnBossKilled;
    MidBossService.CrystalTaken -= PitRoundService.OnCrystalTaken;
  }

  public override void OnStartupServer()
  {
    ServerConVars.TrySet(SkyClipConVar, 0, PitLog);
    PitRoundService.ResetForMap();
  }

  public override void OnClientDisconnect(ClientDisconnectedEvent args)
  {
    if (!args.IsMapChange && args.Controller != null)
      PitRoundService.Forget(args.Controller.PlayerSteamId);
  }

  [GameEventHandler("player_death")]
  public HookResult OnPlayerDeath(PlayerDeathEvent args)
  {
    if (args.UseridController?.As<CCitadelPlayerController>() is { } player)
      PitRoundService.OnDeath(player, args.UseridPawn);

    return HookResult.Continue;
  }

  [GameEventHandler("player_spawn")]
  public HookResult OnPlayerSpawn(PlayerSpawnEvent args)
  {
    if (args.UseridController?.As<CCitadelPlayerController>() is { } player)
      PitRoundService.OnSpawn(player, Timer);

    return HookResult.Continue;
  }

  public override HookResult OnModifyCurrency(ModifyCurrencyEvent args)
  {
    if (!SoulRule.ShouldBlock(args.CurrencyType, args.Source, args.Amount, PitRoundRule.BlocksSouls(PitRoundService.Phase)))
      return HookResult.Continue;

    if (args.Pawn.Controller is { } player)
      PitLog.Debug(player.ToPlayerRef(), "Currency gain blocked Currency={Currency} Amount={Amount} Source={Source}",
        args.CurrencyType, args.Amount, args.Source);

    return HookResult.Stop;
  }

  [Command("pit_start", Description = "Start a pit round: heroes, countdown, boss: pit_start [amber|sapphire] (team in the pit; default alternates)")]
  public void CmdStart(CCitadelPlayerController? caller, string pitTeam = "")
  {
    AdminCommand.Authorize(caller, PitLog, "pit_start");

    var chosen = 0;

    if (pitTeam.Trim().Length > 0 && !PitRoundRule.TryParseTeam(pitTeam, out chosen))
      throw new CommandException("Team must be amber or sapphire (or leave it out to alternate).");

    AdminCommand.Reply(caller, $"[Pit] {PitRoundService.Start(chosen, Timer, ExecutionMode.Debug)}");
  }

  [Command("pit_end", Description = "End the pit round with no winner, clean up the boss and crystals, send everyone up")]
  public void CmdEnd(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, PitLog, "pit_end");

    AdminCommand.Reply(caller, $"[Pit] {PitRoundService.End(PitEndReason.Admin, ExecutionMode.Debug)}");
  }

  [Command("pit_status", Description = "Show the pit round: phase, score, time left, who is in or out, the boss")]
  public void CmdStatus(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, PitLog, "pit_status");

    foreach (var line in PitRoundService.Describe())
      AdminCommand.Reply(caller, $"[Pit] {line}");
  }

  [Command("pit_timing", Description = "Show or set the crystal window and time limit: pit_timing [window 5-120] [limit 30-1800]")]
  public void CmdTiming(CCitadelPlayerController? caller, string window = "", string limit = "")
  {
    AdminCommand.Authorize(caller, PitLog, "pit_timing");

    var newWindow = Seconds(window, PitRoundRule.MinWindowSeconds, PitRoundRule.MaxWindowSeconds, "Window");
    var newLimit = Seconds(limit, PitRoundRule.MinLimitSeconds, PitRoundRule.MaxLimitSeconds, "Limit");

    AdminCommand.Reply(caller, $"[Pit] {PitRoundService.SetTiming(newWindow, newLimit, ExecutionMode.Debug)}");
  }

  private static int? Seconds(string text, int min, int max, string name)
  {
    if (text.Trim().Length == 0)
      return null;

    return PitRoundRule.TryParseSeconds(text, min, max, out var seconds)
      ? seconds
      : throw new CommandException($"{name} must be whole seconds, {min}-{max}.");
  }
}
