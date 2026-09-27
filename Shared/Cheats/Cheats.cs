using DeadworksManaged.Api;

namespace Bublock.Shared;

public static class Cheats
{
  public static void Run(Action action)
  {
    var cheats = ConVar.Find("sv_cheats");

    cheats?.SetInt(1);

    try
    {
      action();
    }
    finally
    {
      cheats?.SetInt(0);
    }
  }
}
