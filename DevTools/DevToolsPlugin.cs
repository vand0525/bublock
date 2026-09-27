using Bublock.Shared;
using DeadworksManaged.Api;

namespace DevTools;

public class DevToolsPlugin : DeadworksPluginBase
{
  private static readonly Logger CommandsLog = BublockLog.For("Commands");

  private static readonly Logger HeroWatchLog = BublockLog.For("HeroWatch");

  private bool _heroWatcherEnabled;
  private int? _heroWatcherPlayerEntityIndex;
  private int? _lastHeroId;
  private IHandle? _heroWatcherTimer;
  private List<(int Index, string DesignerName, string Classname, string Name)>?
    _entitySnapshot;


  public override string Name => "Dev Tools";

  public override void OnLoad(bool isReload)
  {
    BublockLog.Master.Info("Loaded Reload={Reload}", isReload);
  }

  public override void OnUnload()
  {
    _heroWatcherTimer?.Cancel();
    _heroWatcherTimer = null;
  }

  public override void OnStartupServer()
  {
    BublockLog.Master.Info("Initialized. Awaiting commands from authorized users.");
  }

  // SERVER COMMANDS

  [Command("dev_logpath", Description = "Show where this server writes Bublock logs")]
  public void CmdLogPath(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, CommandsLog, "dev_logpath");

    AdminCommand.Reply(
      caller,
      $"[DevTools] Log root: {LogPaths.ResolveRoot()} | " +
      $"DevTools folder: {BublockLog.Directory} | " +
      $"Session: {BublockLog.SessionId} | " +
      $"File logging: {(BublockLog.Hub.FileLoggingEnabled ? "on" : "OFF")}"
    );
  }

  [Command("ent_find", Description = "List entities whose designer, class, or name contains a filter: ent_find <filter>")]
  public void CmdEntFind(CCitadelPlayerController? caller, string filter)
  {
    AdminCommand.Authorize(caller, CommandsLog, "ent_find");

    filter = filter.ToLowerInvariant();

    foreach (var entity in Entities.All)
    {
      var info =
        $"{entity.DesignerName} {entity.Classname} {entity.Name}"
        .ToLowerInvariant();

      if (info.Contains(filter))
      {
        AdminCommand.Reply(
          caller,
          $"[DevTools] {entity.EntityIndex}: " +
          $"{entity.DesignerName} | " +
          $"{entity.Classname} | " +
          $"{entity.Name}"
        );
      }
    }
  }

  [Command("ent_info", Description = "Show fields of every entity with a designer name: ent_info <designerName>")]
  public void CmdEntInfo(CCitadelPlayerController? caller, string designerName)
  {
    AdminCommand.Authorize(caller, CommandsLog, "ent_info");

    var entities = Entities.ByDesignerName(designerName).ToList();

    if (entities.Count == 0)
    {
      AdminCommand.Reply(
        caller,
        $"[DevTools] No entities found with designer name '{designerName}'."
      );
      return;
    }

    AdminCommand.Reply(
      caller,
      $"[DevTools] Found {entities.Count} '{designerName}' entities:"
    );

    foreach (var entity in entities)
    {
      AdminCommand.Reply(caller, "========================================");
      AdminCommand.Reply(caller, $"Name:          {entity.Name}");
      AdminCommand.Reply(caller, $"DesignerName:  {entity.DesignerName}");
      AdminCommand.Reply(caller, $"Classname:     {entity.Classname}");
      AdminCommand.Reply(caller, $"EntityIndex:   {entity.EntityIndex}");
      AdminCommand.Reply(caller, $"EntityHandle:  {entity.EntityHandle}");
      AdminCommand.Reply(caller, $"IsValid:       {entity.IsValid}");
      AdminCommand.Reply(caller, $"Position:      {entity.Position}");
      AdminCommand.Reply(caller, $"Velocity:      {entity.AbsVelocity}");
      AdminCommand.Reply(caller, $"TeamNum:       {entity.TeamNum}");
      AdminCommand.Reply(caller, $"Health:        {entity.Health}");
      AdminCommand.Reply(caller, $"MaxHealth:     {entity.MaxHealth}");
      AdminCommand.Reply(caller, $"LifeState:     {entity.LifeState}");
      AdminCommand.Reply(caller, $"IsAlive:       {entity.IsAlive}");
      AdminCommand.Reply(caller, $"IsOnGround:    {entity.IsOnGround}");

      if (entity.SubclassVData != null)
      {
        AdminCommand.Reply(
          caller,
          $"SubclassVData: {entity.SubclassVData.Name}"
        );
      }

      if (entity.ModifierProp != null)
      {
        AdminCommand.Reply(caller, "ModifierProp:  present");
      }

      if (entity.BodyComponent != null)
      {
        AdminCommand.Reply(caller, "BodyComponent: present");
      }

      AdminCommand.Reply(caller, "========================================");
    }
  }

  [Command("ent_remove", Description = "Remove every entity with a designer name: ent_remove <designerName>")]
  public void CmdEntRemove(CCitadelPlayerController? caller, string designerName)
  {
    AdminCommand.Authorize(caller, CommandsLog, "ent_remove");

    var entities = Entities.ByDesignerName(designerName).ToList();

    if (entities.Count == 0)
    {
      AdminCommand.Reply(
        caller,
        $"[DevTools] No entities found with designer name '{designerName}'."
      );
      return;
    }

    foreach (var entity in entities)
    {
      CommandsLog.Info(
        "Removing entity DesignerName={DesignerName} Classname={Classname} EntityName={EntityName} Index={Index} Position={Position}",
        entity.DesignerName,
        entity.Classname,
        entity.Name,
        entity.EntityIndex,
        entity.Position
      );

      AdminCommand.Reply(
        caller,
        $"[DevTools] Removing: " +
        $"DesignerName={entity.DesignerName}, " +
        $"Classname={entity.Classname}, " +
        $"Name={entity.Name}, " +
        $"Index={entity.EntityIndex}, " +
        $"Position={entity.Position}"
      );

      entity.Remove();
    }

    AdminCommand.Reply(
      caller,
      $"[DevTools] Removed {entities.Count} '{designerName}' entities."
    );
  }

  [Command("dev_herowatch", Description = "Toggle logging of your hero id changes")]
  public void CmdHeroWatch(CCitadelPlayerController caller)
  {
    AdminCommand.Authorize(caller, CommandsLog, "dev_herowatch");

    if (_heroWatcherEnabled)
    {
      StopHeroWatcher(caller);
      return;
    }

    StartHeroWatcher(caller);
  }

  [Command("ent_snapshot", Description = "Remember every entity for a later ent_diff")]
  public void CmdEntSnapshot(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, CommandsLog, "ent_snapshot");

    _entitySnapshot = Entities.All
        .Select(entity => (
          Index: entity.EntityIndex,
          DesignerName: entity.DesignerName,
          Classname: entity.Classname,
          Name: entity.Name
        ))
        .ToList();

    AdminCommand.Reply(
      caller,
      $"[DevTools] Entity snapshot taken | " +
      $"Count={_entitySnapshot.Count}"
    );
  }

  [Command("ent_diff", Description = "List entities added or removed since ent_snapshot")]
  public void CmdEntDiff(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, CommandsLog, "ent_diff");

    if (_entitySnapshot == null)
    {
      AdminCommand.Reply(
        caller,
        "[DevTools] No snapshot exists. Run ent_snapshot first."
      );
      return;
    }

    var currentEntities = Entities.All
        .Select(entity => (
          Index: entity.EntityIndex,
          DesignerName: entity.DesignerName,
          Classname: entity.Classname,
          Name: entity.Name
        ))
        .ToList();

    AdminCommand.Reply(
      caller,
      $"[DevTools] Comparing entities | " +
      $"Before={_entitySnapshot.Count} | " +
      $"Now={currentEntities.Count}"
    );

    foreach (var entity in currentEntities)
    {
      if (_entitySnapshot.Contains(entity))
        continue;

      AdminCommand.Reply(
        caller,
        $"[DevTools] ADDED | " +
        $"Index={entity.Index} | " +
        $"DesignerName={entity.DesignerName} | " +
        $"Classname={entity.Classname} | " +
        $"Name={entity.Name}"
      );
    }

    foreach (var entity in _entitySnapshot)
    {
      if (currentEntities.Contains(entity))
        continue;

      AdminCommand.Reply(
        caller,
        $"[DevTools] REMOVED | " +
        $"Index={entity.Index} | " +
        $"DesignerName={entity.DesignerName} | " +
        $"Classname={entity.Classname} | " +
        $"Name={entity.Name}"
      );
    }
  }

  // PRIVATE METHODS

  // Start watching a player's hero ID
  private void StartHeroWatcher(
    CCitadelPlayerController caller
  )
  {
    _heroWatcherTimer?.Cancel();

    _heroWatcherEnabled = true;
    _heroWatcherPlayerEntityIndex = caller.EntityIndex;
    _lastHeroId = null;

    HeroWatchLog.Info(caller.ToPlayerRef(), "Hero watcher enabled");
    AdminCommand.Reply(
      caller,
      $"[DevTools] Hero watcher ENABLED for " +
      $"{caller.PlayerName}."
    );

    _heroWatcherTimer = Timer.Every(
      0.5.Seconds(),
      () =>
      {
        if (
          !_heroWatcherEnabled ||
          _heroWatcherPlayerEntityIndex == null
        )
        {
          return;
        }

        CCitadelPlayerController? player =
          Players.GetAll().FirstOrDefault(
            player =>
              player.EntityIndex ==
              _heroWatcherPlayerEntityIndex.Value
          );

        if (player == null)
          return;

        CCitadelPlayerPawn? pawn = player.GetHeroPawn();

        if (pawn == null)
          return;

        int heroId = (int)pawn.HeroID;

        if (
          _lastHeroId.HasValue &&
          _lastHeroId.Value == heroId
        )
        {
          return;
        }

        _lastHeroId = heroId;

        HeroWatchLog.Info(
          player.ToPlayerRef(),
          "Hero change HeroId={HeroId} DeadworksName={DeadworksName}",
          heroId,
          pawn.HeroID
        );
        player.PrintToConsole(
          $"[DevTools] HERO CHANGE | " +
          $"player={player.PlayerName} | " +
          $"heroId={heroId} | " +
          $"deadworksName={pawn.HeroID}"
        );
      }
    );
  }

  // Stop hero ID watcher
  private void StopHeroWatcher(CCitadelPlayerController caller)
  {
    _heroWatcherEnabled = false;
    _heroWatcherPlayerEntityIndex = null;
    _lastHeroId = null;

    _heroWatcherTimer?.Cancel();
    _heroWatcherTimer = null;

    HeroWatchLog.Info(caller.ToPlayerRef(), "Hero watcher disabled");
    AdminCommand.Reply(caller, "[DevTools] Hero watcher DISABLED.");
  }
}
