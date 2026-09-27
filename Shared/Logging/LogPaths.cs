using DeadworksManaged.Api;

namespace Bublock.Shared;

public static class LogPaths
{
  public static string ResolveRoot()
  {
    var managedDir = Path.GetDirectoryName(typeof(IDeadworksPlugin).Assembly.Location);

    var baseDir = string.IsNullOrEmpty(managedDir)
      ? AppContext.BaseDirectory
      : Path.Combine(managedDir, "..");

    return Path.GetFullPath(Path.Combine(baseDir, "bublock", "logs"));
  }
}
