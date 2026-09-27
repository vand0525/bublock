using System.Reflection;
using DeadworksManaged.Api;

namespace RiftRoulette.Lobby;

public readonly record struct CommandInfo(string Name, string? Description, bool Hidden);

public static class CommandList
{
  public const string Footer = "Full list: dw_help in console";

  public static IReadOnlyList<string> PlayerCommands(Assembly assembly) =>
    FormatPlayerCommands(Scan(assembly));

  public static IEnumerable<CommandInfo> Scan(Assembly assembly) =>
    assembly.GetTypes()
      .Where(type => !type.IsAbstract && typeof(IDeadworksPlugin).IsAssignableFrom(type))
      .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
      .SelectMany(method => method.GetCustomAttributes<CommandAttribute>())
      .SelectMany(attr => attr.Names.Select(name => new CommandInfo(name, attr.Description, attr.Hidden)));

  // Player commands are the non-hidden names without an underscore; admin
  // names always start with a feature word and an underscore.
  public static IReadOnlyList<string> FormatPlayerCommands(IEnumerable<CommandInfo> commands) =>
    commands
      .Where(command => !command.Hidden && !command.Name.Contains('_'))
      .DistinctBy(command => command.Name, StringComparer.OrdinalIgnoreCase)
      .OrderBy(command => command.Name, StringComparer.OrdinalIgnoreCase)
      .Select(command => string.IsNullOrWhiteSpace(command.Description)
        ? $"/{command.Name}"
        : $"/{command.Name} - {command.Description}")
      .ToList();
}
