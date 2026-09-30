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
    // #region agent log
    Timer.Once(5.Seconds(), () => DumpStringTables("load"));
    Timer.Every(300.Seconds(), () => DumpStringTables("interval"));
    Timer.Once(10.Seconds(), () => MeasureTables("load"));
    // #endregion
  }

  public override void OnUnload()
  {
    // #region agent log
    if (_agentListening)
      Server.RemoveEngineLogListener(OnAgentEngineLog);
    _agentListening = false;
    // #endregion
    _heroWatcherTimer?.Cancel();
    _heroWatcherTimer = null;
  }

  public override void OnStartupServer()
  {
    BublockLog.Master.Info("Initialized. Awaiting commands from authorized users.");
    // #region agent log
    _agentModifierAdds = 0;
    Timer.Once(5.Seconds(), () => DumpStringTables("map-start"));
    // #endregion
  }

  public override HookResult OnAddModifier(AddModifierEvent args)
  {
    ModifierProbe.Observe(args);
    // #region agent log
    _agentModifierAdds++;
    // #endregion
    return HookResult.Continue;
  }

  // #region agent log
  private static readonly Logger AgentLog = BublockLog.For("StringTables");
  private static long _agentModifierAdds;

  // Engine output (table names and entry counts) lands in game/citadel/console.log beside this line's timestamp.
  private void DumpStringTables(string reason)
  {
    AgentLog.Info(
      "[agent] Dumping string tables Reason={Reason} Map={Map} Players={Players} Entities={Entities} ModifierAddsSinceMapStart={ModifierAdds}",
      reason, Server.MapName, Players.GetAll().Count(), Entities.All.Count(), _agentModifierAdds);
    MeasureTables(reason, verbose: false);
  }

  private static readonly string[] AgentTables =
  [
    "genericprecache", "instancebaseline", "lightstyles", "userinfo", "server_query_info", "EntityNames", "EffectDispatch",
    "VguiScreen", "InfoPanel", "Scenes", "AnimTaskTypes", "AnimAssetData", "ActiveModifiers", "ResponseKeys",
  ];

  private static readonly System.Text.RegularExpressions.Regex AgentHeader = new(@"table\s+(\S+)\s+(\d+)\s*$");
  private static readonly System.Text.RegularExpressions.Regex AgentField = new(@":\s+(\d+) : \S+ bits (\d+) to (\d+) \[");
  private static readonly System.Text.RegularExpressions.Regex AgentString = new(@"^\s*(\d+) : (.*)$");
  private static readonly System.Text.RegularExpressions.Regex AgentTooBig = new(@"Message size (\d+) is too big");

  private sealed class AgentTableBytes
  {
    public int Entries;
    public long StringBytes;
    public long DataBytes;
    public long BaselineBits;
    public long EntryBits;
  }

  private static readonly Dictionary<string, AgentTableBytes> _agentBytes = [];
  private static AgentTableBytes? _agentCurrent;
  private static bool _agentMeasuring;
  private static bool _agentListening;

  // The engine prints verbose dumps line by line; this adds them up while a measurement runs and watches for failed joins.
  private static int _agentLinesSeen;
  private static int _agentSamples;

  private void OnAgentEngineLog(string message)
  {
    if (_agentMeasuring)
    {
      _agentLinesSeen++;
      if (_agentSamples < 8 && (message.Contains("table") || message.Contains(" : ")))
      {
        _agentSamples++;
        AgentLog.Info("[agent] Engine line sample Raw={Raw}", message.Replace("\n", "\\n").Replace("\r", "\\r"));
      }
    }

    var tooBig = AgentTooBig.Match(message);

    if (tooBig.Success)
    {
      AgentLog.Warn("[agent] Join message too big Size={Size}", tooBig.Groups[1].Value);
      Timer.Once(1.Seconds(), () => MeasureTables("join-failed"));
      return;
    }

    if (!_agentMeasuring)
      return;

    var text = message;
    var tag = text.IndexOf("[stringtables]", StringComparison.Ordinal);
    if (tag >= 0)
      text = text[(tag + "[stringtables]".Length)..];
    text = text.TrimEnd('\r', '\n');

    var header = AgentHeader.Match(text);
    if (header.Success)
    {
      FlushAgentEntry();
      var name = header.Groups[1].Value;
      _agentCurrent = new AgentTableBytes { Entries = int.Parse(header.Groups[2].Value) };
      _agentBytes[name] = _agentCurrent;
      _agentCurrentName = name;
      return;
    }

    if (_agentCurrent == null)
      return;

    var field = AgentField.Match(text);
    if (field.Success)
    {
      if (field.Groups[1].Value == "0")
        FlushAgentEntry();
      _agentCurrent.EntryBits = Math.Max(_agentCurrent.EntryBits, long.Parse(field.Groups[3].Value));
      return;
    }

    var trimmed = text.TrimStart();
    if (trimmed.StartsWith(">>", StringComparison.Ordinal))
    {
      var tokens = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      var groups = 0;
      for (var i = tokens.Length - 1; i > 0 && groups < 6; i--, groups++)
      {
        var token = tokens[i];
        if (token.Length is < 2 or > 8 || token.Length % 2 != 0 || !token.All(Uri.IsHexDigit))
          break;
        _agentCurrent.DataBytes += token.Length / 2;
      }
      return;
    }

    var entry = AgentString.Match(text);
    if (entry.Success)
    {
      _agentCurrent.StringBytes += entry.Groups[2].Value.Length + 1;
      if (ReferenceEquals(_agentCurrent, _agentBytes.GetValueOrDefault("AnimAssetData")))
        _agentAnimEntries.Add((entry.Groups[2].Value.Trim(), _agentCurrent.DataBytes));
      if (_agentCurrentName != null && _agentNames.TryGetValue(_agentCurrentName, out var names))
        names.Add(entry.Groups[2].Value.Trim());
    }
  }

  private static readonly List<(string Name, long DataBefore)> _agentAnimEntries = [];
  private static string? _agentCurrentName;

  // Entry names per table from the current verbose dump, and from the previous one (to log only what is new).
  private static readonly string[] AgentNameTables = ["AnimAssetData", "EntityNames"];
  private static readonly Dictionary<string, HashSet<string>> _agentNames = [];
  private static readonly Dictionary<string, HashSet<string>> _agentPreviousNames = [];
  private const int AgentNewNamesLimit = 300;

  private static void LogNewNames(string reason)
  {
    foreach (var table in AgentNameTables)
    {
      if (!_agentNames.TryGetValue(table, out var names))
        continue;

      if (!_agentPreviousNames.TryGetValue(table, out var previous))
      {
        AgentLog.Info("[agent] Names saved for next diff Reason={Reason} Table={Table} Count={Count}", reason, table, names.Count);
      }
      else
      {
        var added = names.Where(name => !previous.Contains(name)).ToList();
        AgentLog.Info("[agent] New names Reason={Reason} Table={Table} Added={Added} Count={Count}", reason, table, added.Count, names.Count);

        foreach (var name in added.Take(AgentNewNamesLimit))
          AgentLog.Info("[agent] New name Reason={Reason} Table={Table} Name={Name}", reason, table, name);
      }

      _agentPreviousNames[table] = [.. names];
    }
  }

  private static void FlushAgentEntry()
  {
    if (_agentCurrent == null || _agentCurrent.EntryBits == 0)
      return;
    _agentCurrent.BaselineBits += _agentCurrent.EntryBits;
    _agentCurrent.EntryBits = 0;
  }

  private void MeasureTables(string reason, bool verbose = true)
  {
    if (_agentMeasuring)
      return;

    if (!_agentListening)
    {
      Server.AddEngineLogListener(OnAgentEngineLog);
      _agentListening = true;
    }

    _agentBytes.Clear();
    _agentAnimEntries.Clear();
    _agentCurrent = null;
    _agentCurrentName = null;
    _agentNames.Clear();
    if (verbose)
    {
      foreach (var table in AgentNameTables)
        _agentNames[table] = [];
    }
    _agentMeasuring = true;

    if (verbose)
    {
      foreach (var table in AgentTables)
        Server.ExecuteCommand($"dumpstringtable {table} sv verbose");
    }
    else
    {
      Server.ExecuteCommand("dumpstringtable all sv simple");
    }

    Timer.Once(3.Seconds(), () =>
    {
      FlushAgentEntry();
      _agentMeasuring = false;

      if (!verbose)
      {
        AgentLog.Info("[agent] Table counts Reason={Reason} AnimAssetData={Anim} instancebaseline={Baseline} EntityNames={Names} ActiveModifiers={Modifiers} Players={Players}",
          reason, _agentBytes.GetValueOrDefault("AnimAssetData")?.Entries, _agentBytes.GetValueOrDefault("instancebaseline")?.Entries,
          _agentBytes.GetValueOrDefault("EntityNames")?.Entries, _agentBytes.GetValueOrDefault("ActiveModifiers")?.Entries, Players.GetAll().Count());
        _agentLinesSeen = 0;
        _agentSamples = 0;
        return;
      }

      if (_agentBytes.TryGetValue("AnimAssetData", out var anim))
      {
        for (var i = 0; i < _agentAnimEntries.Count; i++)
        {
          var end = i + 1 < _agentAnimEntries.Count ? _agentAnimEntries[i + 1].DataBefore : anim.DataBytes;
          AgentLog.Info("[agent] Anim asset Reason={Reason} Index={Index} Bytes={Bytes} Name={Name}",
            reason, i, end - _agentAnimEntries[i].DataBefore, _agentAnimEntries[i].Name);
        }
      }
      long total = 0;

      foreach (var (name, bytes) in _agentBytes.OrderByDescending(entry => entry.Value.StringBytes + entry.Value.DataBytes + entry.Value.BaselineBits / 8))
      {
        var tableBytes = bytes.StringBytes + bytes.DataBytes + bytes.BaselineBits / 8;
        total += tableBytes;
        AgentLog.Info("[agent] Table bytes Reason={Reason} Table={Table} Entries={Entries} Total={Total} Strings={Strings} Data={Data} Baselines={Baselines}",
          reason, name, bytes.Entries, tableBytes, bytes.StringBytes, bytes.DataBytes, bytes.BaselineBits / 8);
      }

      AgentLog.Info("[agent] Table bytes total Reason={Reason} Tables={Tables} TotalBytes={Total} Players={Players} LinesSeen={Lines}",
        reason, _agentBytes.Count, total, Players.GetAll().Count(), _agentLinesSeen);
      LogNewNames(reason);
      _agentLinesSeen = 0;
      _agentSamples = 0;
    });
  }

  [Command("dev_tables", Hidden = true, Description = "Probe: verbose string table dump (bytes per table, new names since the last dump)")]
  public void CmdTables(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, CommandsLog, "dev_tables");
    MeasureTables("manual");
    AdminCommand.Reply(caller, "[DevTools] Measuring string tables; results in stringtables log in ~3 s.");
  }
  // #endregion

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
