using DeadworksManaged.Api;
using RiftRoulette.Lobby;

namespace Bublock.Tests.RiftRoulette;

public class CommandListTests
{
  [Fact]
  public void FormatPlayerCommands_keeps_visible_names_without_underscore_sorted()
  {
    var lines = CommandList.FormatPlayerCommands(
    [
      new CommandInfo("status", "Show your status", false),
      new CommandInfo("rift_start", "Start the next rift", false),
      new CommandInfo("pick", "Draft a hero: pick <hero>", false),
      new CommandInfo("secret", "Hidden thing", true),
      new CommandInfo("heroes", null, false),
    ]);

    Assert.Equal(
      ["/heroes", "/pick - Draft a hero: pick <hero>", "/status - Show your status"],
      lines);
  }

  [Fact]
  public void FormatPlayerCommands_lists_each_name_once()
  {
    var lines = CommandList.FormatPlayerCommands(
    [
      new CommandInfo("pick", "Draft a hero", false),
      new CommandInfo("Pick", "Draft a hero", false),
    ]);

    Assert.Single(lines);
  }

  [Fact]
  public void Scan_reads_command_attributes_on_plugin_classes()
  {
    var commands = CommandList.Scan(typeof(CommandListTests).Assembly).ToList();

    Assert.Contains(new CommandInfo("sample", "Sample player command", false), commands);
    Assert.Contains(new CommandInfo("sample_admin", "Sample admin command", false), commands);
    Assert.Contains(commands, command => command is { Name: "sample_hidden", Hidden: true });
  }

  public class SamplePlugin : DeadworksPluginBase
  {
    public override string Name => "Sample";

    [Command("sample", Description = "Sample player command")]
    public void CmdSample(CCitadelPlayerController player) { }

    [Command("sample_admin", Description = "Sample admin command")]
    public void CmdSampleAdmin(CCitadelPlayerController? caller) { }

    [Command("sample_hidden", Hidden = true)]
    public void CmdSampleHidden(CCitadelPlayerController player) { }
  }
}
