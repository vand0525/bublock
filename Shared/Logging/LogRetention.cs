using System.Globalization;
using System.Text.RegularExpressions;

namespace Bublock.Shared;

public static partial class LogRetention
{
  public const int DefaultDays = 7;

  [GeneratedRegex(@"-(?<date>\d{8})(\.\d+)?\.log$")]
  private static partial Regex DatedLogName();

  public static int DeleteOlderThan(string directory, DateTime utcNow, int days = DefaultDays)
  {
    if (!Directory.Exists(directory))
      return 0;

    var cutoff = utcNow.ToUniversalTime().Date.AddDays(-days);
    var deleted = 0;

    foreach (var path in Directory.EnumerateFiles(directory, "*.log"))
    {
      var match = DatedLogName().Match(Path.GetFileName(path));

      if (!match.Success)
        continue;

      if (!DateTime.TryParseExact(
            match.Groups["date"].Value,
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var fileDate))
        continue;

      if (fileDate >= cutoff)
        continue;

      try
      {
        File.Delete(path);
        deleted++;
      }
      catch (IOException)
      {
      }
      catch (UnauthorizedAccessException)
      {
      }
    }

    return deleted;
  }
}
