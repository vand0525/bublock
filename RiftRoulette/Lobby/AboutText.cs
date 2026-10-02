using RiftRoulette.Betting;
using RiftRoulette.RandomMode;

namespace RiftRoulette.Lobby;

public static class AboutText
{
  public static IReadOnlyList<string> MirrorLines() =>
  [
    "Rift Roulette mirror mode: teams fight round after round for the rift, and everyone plays the same hero with the same build.",
    "Teams stay even: with an odd number of players, one sits out each round and watches.",
    "/commands lists every command."
  ];

  public static IReadOnlyList<string> Lines(int bettingCloseSeconds) =>
  [
    "Rift Roulette: teams fight round after round for the rift. Every round you get a random hero with one of its top builds.",
    "Teams stay even: with an odd number of players, one sits out each round and watches.",
    $"Betting souls are separate from your in-game souls. You start with {BetBoardText.Format(BetBook.StartingChips)}, " +
    $"a kill gives {BetBoardText.Format(BetBook.ChipsPerKill)} and an assist gives {BetBoardText.Format(BetBook.ChipsPerAssist)}.",
    $"Bet all your souls on the next round: type sapphire or amber (or /bet <team>). Betting opens between rounds " +
    $"and closes {bettingCloseSeconds}s into the round. A win doubles your stake.",
    "If you're fighting you can only bet on your own team; the player sitting out can bet on either.",
    $"Spend souls: /reserve <hero> ({BetBoardText.Format(HeroReservations.Cost)}) gives you that hero for your next {HeroReservations.Rounds} rounds; " +
    $"/heroban <hero> ({BetBoardText.Format(HeroBans.Cost)}) takes a hero out for both teams for one round; " +
    $"/mark <slot> ({BetBoardText.Format(MarkBook.Cost)}) marks an enemy fighter: kill them that round to steal a quarter of their souls.",
    "/souls shows your souls; /commands lists every command."
  ];
}
