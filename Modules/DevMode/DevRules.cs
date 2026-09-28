namespace Bublock.Modules.DevMode;

public enum RunMode
{
  Dev,
  Prod
}

public static class DevRules
{
  // A dev-only command in prod gets a refusal to show the caller; otherwise null (allowed).
  public static string? Refusal(RunMode mode, string command) =>
    mode == RunMode.Prod ? $"{command} is dev-only; a live session is running (/stop first)." : null;

  public static string Name(RunMode mode) => mode == RunMode.Dev ? "dev" : "prod";

  public static bool TryParse(string text, out RunMode mode)
  {
    switch (text.Trim().ToLowerInvariant())
    {
      case "dev":
        mode = RunMode.Dev;
        return true;
      case "prod":
        mode = RunMode.Prod;
        return true;
      default:
        mode = RunMode.Dev;
        return false;
    }
  }
}
