using System.Numerics;
using Bublock.Shared;
using DeadworksManaged.Api;

namespace Bublock.Modules.Movement;

public class MovementPlugin : DeadworksPluginBase
{
  private static readonly Logger CommandsLog = BublockLog.For("Movement");

  public override string Name => "Movement";

  [Command("mv_list", Description = "List named teleport locations")]
  public void CmdList(CCitadelPlayerController? caller)
  {
    AdminCommand.Authorize(caller, CommandsLog, "mv_list");

    var locations = MovementService.Locations.List();
    AdminCommand.Reply(caller, $"[Movement] {locations.Count} location(s)");

    foreach (var listing in locations)
    {
      var location = listing.Location;
      AdminCommand.Reply(
        caller,
        $"[Movement] {location.Name} | Position={location.Position} | Angle={location.Angle}" +
        (listing.Saved ? " | saved" : ""));
    }
  }

  [Command("mv_where", Description = "Show a player's position and view angle: mv_where [slot]")]
  public void CmdWhere(CCitadelPlayerController? caller, int slot = -1)
  {
    AdminCommand.Authorize(caller, CommandsLog, "mv_where");

    var player = ResolvePlayer(caller, slot);
    var where = MovementService.Where(player)
      ?? throw new CommandException($"Slot {player.Slot} has no hero.");

    AdminCommand.Reply(
      caller,
      $"[Movement] {player.PlayerName} (slot {player.Slot}) | Position={where.Position} | Angle={where.EyeAngles}");
  }

  [Command("mv_tp", Description = "Teleport yourself or a slot to a location: mv_tp <location> [slot]")]
  public void CmdTeleport(CCitadelPlayerController? caller, string location, int slot = -1)
  {
    AdminCommand.Authorize(caller, CommandsLog, "mv_tp");

    var target = ResolveLocation(location);
    var player = ResolvePlayer(caller, slot);

    if (!MovementService.TeleportTo(player, target, ExecutionMode.Debug))
      throw new CommandException($"Slot {player.Slot} has no hero.");

    AdminCommand.Reply(caller, $"[Movement] Moved {player.PlayerName} to {target.Name}");
  }

  [Command("mv_tp_team", Description = "Teleport a team to a location: mv_tp_team <team number> <location>")]
  public void CmdTeleportTeam(CCitadelPlayerController? caller, int team, string location)
  {
    AdminCommand.Authorize(caller, CommandsLog, "mv_tp_team");

    var target = ResolveLocation(location);
    var players = Players.GetAll().Where(player => player.TeamNum == team).ToList();
    var moved = MovementService.TeleportPlayers(players, target, ExecutionMode.Debug);

    AdminCommand.Reply(caller, $"[Movement] Moved {moved} of {players.Count} player(s) on team {team} to {target.Name}");
  }

  [Command("mv_tp_all", Description = "Teleport every player to a location: mv_tp_all <location>")]
  public void CmdTeleportAll(CCitadelPlayerController? caller, string location)
  {
    AdminCommand.Authorize(caller, CommandsLog, "mv_tp_all");

    var target = ResolveLocation(location);
    var players = Players.GetAll().ToList();
    var moved = MovementService.TeleportPlayers(players, target, ExecutionMode.Debug);

    AdminCommand.Reply(caller, $"[Movement] Moved {moved} of {players.Count} player(s) to {target.Name}");
  }

  [Command("mv_angle", Description = "Set a camera angle: mv_angle <pitch> <yaw> <roll> [slot]")]
  public void CmdAngle(CCitadelPlayerController? caller, float pitch, float yaw, float roll, int slot = -1)
  {
    AdminCommand.Authorize(caller, CommandsLog, "mv_angle");

    var player = ResolvePlayer(caller, slot);
    var angle = new Vector3(pitch, yaw, roll);

    MovementService.SetViewAngle(player, angle);
    CommandsLog.WithMode(ExecutionMode.Debug).Debug(player.ToPlayerRef(), "View angle set Angle={Angle}", angle);

    AdminCommand.Reply(caller, $"[Movement] Set {player.PlayerName}'s camera to {angle}");
  }

  [Command("mv_save", Description = "Save your position and view as a location until reload: mv_save <name>")]
  public void CmdSave(CCitadelPlayerController? caller, string name)
  {
    AdminCommand.Authorize(caller, CommandsLog, "mv_save");

    if (!LocationRegistry.IsValidName(name))
      throw new CommandException("Location names use letters, digits, _ . - only.");

    if (MovementService.Locations.TryGet(name, out _) && !MovementService.Locations.IsSaved(name))
      throw new CommandException($"'{name}' is a built-in location and cannot be replaced.");

    var player = caller ?? throw new CommandException("Run mv_save in game.");
    var where = MovementService.Where(player)
      ?? throw new CommandException("You need a hero to save a location.");

    var location = new MovementLocation(name, where.Position, new Vector3(where.EyeAngles.X, where.EyeAngles.Y, 0f));
    MovementService.Locations.Register(location, saved: true);

    CommandsLog.Info(player.ToPlayerRef(), "Location saved Location={Location} Position={Position} Angle={Angle}", name, location.Position, location.Angle);
    AdminCommand.Reply(caller, $"[Movement] Saved {name} at {location.Position}");
  }

  [Command("mv_remove", Description = "Remove a location saved with mv_save: mv_remove <name>")]
  public void CmdRemove(CCitadelPlayerController? caller, string name)
  {
    AdminCommand.Authorize(caller, CommandsLog, "mv_remove");

    if (!MovementService.Locations.Unregister(name))
    {
      throw new CommandException(MovementService.Locations.TryGet(name, out _)
        ? $"'{name}' is a built-in location and cannot be removed."
        : $"No saved location named '{name}'. See mv_list.");
    }

    CommandsLog.Info("Location removed Location={Location}", name);
    AdminCommand.Reply(caller, $"[Movement] Removed {name}");
  }

  private static MovementLocation ResolveLocation(string name) =>
    MovementService.Locations.TryGet(name, out var location)
      ? location
      : throw new CommandException($"No location named '{name}'. See mv_list.");

  private static CCitadelPlayerController ResolvePlayer(CCitadelPlayerController? caller, int slot)
  {
    if (slot < 0)
      return caller ?? throw new CommandException("Give a slot when running from the server console.");

    return Players.GetAll().FirstOrDefault(player => player.Slot == slot)
      ?? throw new CommandException($"No player in slot {slot}.");
  }
}
