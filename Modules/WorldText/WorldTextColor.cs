namespace Bublock.Modules.WorldText;

public readonly record struct WorldTextColor(byte R, byte G, byte B, byte A = 255)
{
  public static readonly WorldTextColor White = new(255, 255, 255, 255);
}
