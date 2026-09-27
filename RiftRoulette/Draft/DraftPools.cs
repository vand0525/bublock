using DeadworksManaged.Api;
using RiftRoulette.Lobby;

namespace RiftRoulette.Draft;

public static class DraftPools
{
  public static IReadOnlyList<Heroes> Sapphire { get; } =
  [
    Heroes.Shiv,
    Heroes.Yamato,
    Heroes.Mirage,
    Heroes.Wraith,
    Heroes.Krill,
    Heroes.Viper
  ];

  public static IReadOnlyList<Heroes> Amber { get; } =
  [
    Heroes.Fencer,
    Heroes.Drifter,
    Heroes.Doorman,
    Heroes.Werewolf,
    Heroes.PunkGoat,
    Heroes.Magician
  ];

  public static int TeamOf(Heroes hero) =>
    Sapphire.Contains(hero)
      ? RiftRouletteTeams.Sapphire
      : Amber.Contains(hero)
        ? RiftRouletteTeams.Amber
        : 0;
}
