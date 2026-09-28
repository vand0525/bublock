using Bublock.Shared;
using DeadworksManaged.Api;

namespace CleanSlate;

public class CleanSlatePlugin : DeadworksPluginBase
{
    private static readonly Logger CleanupLog = BublockLog.For("Cleanup");

    public override string Name => "Clean Slate";

    public override void OnLoad(bool isReload)
    {
        BublockLog.Master.Info("Loaded Reload={Reload}", isReload);

        if (isReload)
            RunStartup(isReload: true);
    }

    public override void OnStartupServer()
    {
        RunStartup(isReload: false);
    }

    [Command("cleanup_run", Description = "Re-apply the spawn convars, remove lane bosses, spawners and shop kiosks, disable shop zones")]
    public void CmdCleanupRun(CCitadelPlayerController? caller)
    {
        AdminCommand.Authorize(caller, CleanupLog, "cleanup_run");

        CleanSlateService.ApplyConvars(ExecutionMode.Debug);
        var result = CleanSlateService.RemoveMapEntities(ExecutionMode.Debug);

        BublockLog.Master.Info("Map cleanup re-run {Summary}", result.Describe());
        AdminCommand.Reply(
            caller,
            $"[CleanSlate] Convars applied, {result.RemovedCount} entities removed, {result.DisabledCount} disabled");
    }

    // After a changelevel the lane guardians (npc_trooper_boss) spawn later than 2 s, so later passes catch them.
    public static readonly int[] PassSeconds = [2, 10, 30];

    private void RunStartup(bool isReload)
    {
        CleanSlateService.ApplyConvars();

        foreach (var seconds in PassSeconds)
        {
            Timer.Once(seconds.Seconds(), () =>
            {
                var result = CleanSlateService.RemoveMapEntities();

                if (seconds == PassSeconds[0] || result.RemovedCount > 0)
                    BublockLog.Master.Info("Map cleanup complete Reload={Reload} Pass={Pass} {Summary}", isReload, seconds, result.Describe());
            });
        }
    }
}
