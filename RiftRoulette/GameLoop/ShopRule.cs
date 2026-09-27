namespace RiftRoulette.GameLoop;

public static class ShopRule
{
  public static bool BuyAnywhere(bool isDuel, bool matchRunning) => isDuel && !matchRunning;
}
