using DeadworksManaged.Api;

namespace Bublock.Shared;

public static class ServerConVars
{
  private static readonly HashSet<string> ReportedMissing = [];

  public static bool TrySet(string name, int value, Logger log)
  {
    var convar = ConVar.Find(name);

    if (convar == null)
    {
      if (ReportedMissing.Add(name))
        log.Warn("Convar missing, value not applied Name={Name} Value={Value}", name, value);

      return false;
    }

    convar.SetInt(value);
    return true;
  }
}
