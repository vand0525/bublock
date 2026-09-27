namespace CleanSlate;

public sealed record CleanupResult(
    IReadOnlyDictionary<string, int> Removed,
    IReadOnlyDictionary<string, int> Disabled)
{
    public int RemovedCount => Removed.Values.Sum();

    public int DisabledCount => Disabled.Values.Sum();

    public string Describe() =>
        $"Removed=[{Join(Removed)}] Disabled=[{Join(Disabled)}]";

    private static string Join(IReadOnlyDictionary<string, int> counts) =>
        counts.Count == 0
            ? "none"
            : string.Join(" ", counts.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"));
}
