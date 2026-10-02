using Bublock.Modules.Restraint;
using Bublock.Shared;
using DeadworksManaged.Api;
using RiftRoulette.Round;
using RiftRoulette.SelfTest;
using RiftRoulette.Stats;

namespace RiftRoulette.GameLoop;

public class GameLoopPlugin : DeadworksPluginBase
{
  private static readonly Logger MatchLog = BublockLog.For("Match");

  private const int ReloadCheckSeconds = 3;

  public override string Name => "Rift Roulette Game Loop";

  public override void OnLoad(bool isReload)
  {
    Timer.Once(ReloadCheckSeconds.Seconds(), () => AutoStartService.Check(Timer));
  }

  public override void OnGameFrame(bool simulating, bool firstTick, bool lastTick)
  {
    EventCounters.Hit("game_frame");

    if (simulating)
      WatchGuard.Tick();
  }

  public override HookResult OnClientConCommand(ClientConCommandEvent args)
  {
    var player = args.Controller;

    if (player != null && RestraintService.IsRestrained(player.PlayerSteamId))
      WatchGuard.LogCommand(player, args.Command, args.Args);

    return HookResult.Continue;
  }

  public override HookResult OnTakeDamage(TakeDamageEvent args)
  {
    EventCounters.Hit("take_damage");

    if (!RestraintService.IsRestrainedPawn(args.Entity))
    {
      StatsService.RecordDamage(args);
      return HookResult.Continue;
    }

    EventCounters.Hit("damage_blocked_restrained");
    return HookResult.Stop;
  }

  public override HookResult OnModifyCurrency(ModifyCurrencyEvent args)
  {
    EventCounters.Hit("modify_currency");

    if (!SoulRule.ShouldBlock(
          args.CurrencyType, args.Source, args.Amount, MatchService.State.IsRunning))
      return HookResult.Continue;

    EventCounters.Hit(args.CurrencyType == ECurrencyType.EGold
      ? $"soul_blocked_{args.Source}"
      : $"ability_blocked_{args.CurrencyType}_{args.Source}");

    var player = args.Pawn.Controller;

    if (player != null)
    {
      MatchLog.Debug(
        player.ToPlayerRef(),
        "Currency gain blocked Currency={Currency} Amount={Amount} Source={Source}",
        args.CurrencyType, args.Amount, args.Source);
    }
    else
    {
      MatchLog.Debug(
        "Currency gain blocked Currency={Currency} Amount={Amount} Source={Source}",
        args.CurrencyType, args.Amount, args.Source);
    }

    return HookResult.Stop;
  }

  [Command("match_start", Description = "Start a continuous match; rounds then run themselves")]
  public void CmdMatchStart(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, MatchLog, "match_start");

    AdminCommand.Reply(caller, $"[Match] {MatchService.Start(Timer, ExecutionMode.Debug)}");
  }

  [Command("match_end", Description = "End the match, show the final score, and return everyone to the lobby")]
  public void CmdMatchEnd(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, MatchLog, "match_end");

    AdminCommand.Reply(caller, $"[Match] {MatchService.End(Timer, ExecutionMode.Debug)}");
  }

  [Command("match_auto", Description = "Turn match auto-start on or off: match_auto <on|off>")]
  public void CmdMatchAuto(CCitadelPlayerController? caller, string state)
  {
    AdminCommand.Authorize(caller, MatchLog, "match_auto");

    var enabled = state.ToLowerInvariant() switch
    {
      "on" => true,
      "off" => false,
      _ => throw new CommandException("Use match_auto on or match_auto off.")
    };

    AutoStartService.SetEnabled(enabled, ExecutionMode.Debug);

    var action = enabled ? AutoStartService.Check(Timer, ExecutionMode.Debug) : AutoStartAction.None;
    var note = action == AutoStartAction.Start ? " - match started" : "";

    AdminCommand.Reply(caller, $"[Match] Auto-start {(enabled ? "on" : "off")}{note}");
  }

  [Command("match_status", Description = "Show match phase, round, score, and intermission length")]
  public void CmdMatchStatus(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, MatchLog, "match_status");

    foreach (var line in MatchService.DescribeMatch())
      AdminCommand.Reply(caller, $"[Match] {line}");
  }

  [Command("match_intermission", Description = "Set the seconds between rounds: match_intermission <seconds>")]
  public void CmdMatchIntermission(CCitadelPlayerController? caller, int seconds)
  {
    AdminCommand.Authorize(caller, MatchLog, "match_intermission");

    if (!MatchService.SetIntermission(seconds, ExecutionMode.Debug))
    {
      throw new CommandException(
        $"Intermission must be {MatchService.MinIntermissionSeconds}-{MatchService.MaxIntermissionSeconds} seconds.");
    }

    AdminCommand.Reply(caller, $"[Match] Intermission set to {seconds}s (applies from the next countdown)");
  }

  [Command("match_mode", Description = "Set how heroes are chosen: match_mode <random|mirror>")]
  public void CmdMatchMode(CCitadelPlayerController? caller, string heroMode)
  {
    AdminCommand.Authorize(caller, MatchLog, "match_mode");

    if (!MatchConfig.TryParseHeroMode(heroMode, out var parsed))
      throw new CommandException($"Mode must be {MatchConfig.Names<HeroMode>()}.");

    AdminCommand.Reply(caller, $"[Match] {MatchService.SetHeroMode(parsed, Timer, ExecutionMode.Debug)}");
  }

  [Command("match_format", Description = "Set the match format: match_format <continuous>")]
  public void CmdMatchFormat(CCitadelPlayerController? caller, string format)
  {
    AdminCommand.Authorize(caller, MatchLog, "match_format");

    if (!MatchConfig.TryParseFormat(format, out var parsed))
      throw new CommandException($"Format must be {MatchConfig.Names<MatchFormat>()}.");

    AdminCommand.Reply(caller, $"[Match] {MatchService.SetFormat(parsed, ExecutionMode.Debug)}");
  }

  [Command("match_config", Description = "Show the match configuration (hero mode, format, intermission)")]
  public void CmdMatchConfig(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, MatchLog, "match_config");

    foreach (var line in MatchService.DescribeConfig())
      AdminCommand.Reply(caller, $"[Match] {line}");
  }

  [Command("score", Description = "Show the match score")]
  public void CmdScore(CCitadelPlayerController player)
  {
    foreach (var line in MatchService.DescribeScore())
      PlayerChat.Send(player, line);
  }
}
