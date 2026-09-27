using System.Globalization;
using System.Text;

namespace Bublock.Shared;

public static class LogFormatter
{
  public static string Format(
    DateTime utcNow,
    LogLevel level,
    string prefix,
    string sessionId,
    string? roundId,
    PlayerRef? player,
    string message,
    Exception? exception = null)
  {
    var builder = new StringBuilder(128);

    builder
      .Append(utcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture))
      .Append(" [").Append(level).Append("] [").Append(prefix).Append(']')
      .Append(" session=").Append(sessionId)
      .Append(" round=").Append(string.IsNullOrEmpty(roundId) ? "-" : roundId);

    if (player is { } p)
    {
      builder
        .Append(" player=\"").Append(p.Name.Replace('"', '\'')).Append('"')
        .Append(" steam=").Append(p.SteamId.ToString(CultureInfo.InvariantCulture))
        .Append(" slot=").Append(p.Slot.ToString(CultureInfo.InvariantCulture));
    }

    builder.Append(" | ").Append(message);

    if (exception != null)
      builder.Append(Environment.NewLine).Append(exception);

    return builder.ToString();
  }

  public static string RenderTemplate(string template, IReadOnlyList<object?> args)
  {
    var builder = new StringBuilder(template.Length + 32);
    var argIndex = 0;

    for (var i = 0; i < template.Length; i++)
    {
      var c = template[i];

      if (c == '{' && i + 1 < template.Length && template[i + 1] == '{')
      {
        builder.Append('{');
        i++;
        continue;
      }

      if (c == '}' && i + 1 < template.Length && template[i + 1] == '}')
      {
        builder.Append('}');
        i++;
        continue;
      }

      if (c == '{')
      {
        var close = template.IndexOf('}', i + 1);

        if (close > i + 1)
        {
          var name = template.Substring(i + 1, close - i - 1);

          if (argIndex < args.Count)
          {
            if (!EndsWithNameEquals(builder, name))
              builder.Append(name).Append('=');

            builder.Append(FormatValue(args[argIndex]));
            argIndex++;
          }
          else
          {
            builder.Append('{').Append(name).Append('}');
          }

          i = close;
          continue;
        }
      }

      builder.Append(c);
    }

    return builder.ToString();
  }

  private static bool EndsWithNameEquals(StringBuilder builder, string name)
  {
    var length = name.Length + 1;

    if (builder.Length < length || builder[^1] != '=')
      return false;

    for (var i = 0; i < name.Length; i++)
    {
      if (builder[builder.Length - length + i] != name[i])
        return false;
    }

    var before = builder.Length - length - 1;
    return before < 0 || !char.IsLetterOrDigit(builder[before]);
  }

  public static string FormatValue(object? value)
  {
    var text = value switch
    {
      null => "null",
      string s => s,
      IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
      _ => value.ToString() ?? ""
    };

    if (value is string && (text.Length == 0 || text.Any(char.IsWhiteSpace)))
      return "\"" + text.Replace('"', '\'') + "\"";

    return text;
  }
}
