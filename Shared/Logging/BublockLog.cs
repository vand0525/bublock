using System.Runtime.Loader;

namespace Bublock.Shared;

public static class BublockLog
{
  private static readonly Lazy<LogHub> LazyHub = new(CreateHub);

  public static LogHub Hub => LazyHub.Value;
  public static Logger Master => Hub.Master;
  public static string SessionId => Hub.SessionId;
  public static string Directory => Hub.Directory;

  public static string? RoundId
  {
    get => Hub.RoundId;
    set => Hub.RoundId = value;
  }

  public static Logger For(string feature) => Hub.For(feature);

  private static LogHub CreateHub()
  {
    var assembly = typeof(BublockLog).Assembly;
    var dllTag = assembly.GetName().Name ?? "Bublock";

    string directory;

    try
    {
      directory = Path.Combine(LogPaths.ResolveRoot(), dllTag);
    }
    catch
    {
      directory = Path.Combine(AppContext.BaseDirectory, "bublock", "logs", dllTag);
    }

    var hub = new LogHub(dllTag, directory);

    var context = AssemblyLoadContext.GetLoadContext(assembly);

    if (context != null)
      context.Unloading += _ => hub.Dispose();

    return hub;
  }
}
