namespace RiftRoulette.Stats;

public sealed class DamageLedger
{
  public const double AssistFraction = 0.2;

  private readonly Dictionary<ulong, Dictionary<ulong, double>> _byVictim = [];

  public void Record(ulong victimId, ulong attackerId, double amount)
  {
    if (victimId == 0 || attackerId == 0 || victimId == attackerId || amount <= 0)
      return;

    if (!_byVictim.TryGetValue(victimId, out var attackers))
      _byVictim[victimId] = attackers = [];

    attackers[attackerId] = attackers.GetValueOrDefault(attackerId) + amount;
  }

  public IReadOnlyDictionary<ulong, double> DamageTo(ulong victimId) =>
    _byVictim.TryGetValue(victimId, out var attackers) ? attackers : new Dictionary<ulong, double>();

  public IReadOnlyList<ulong> Assisters(ulong victimId, ulong killerId, int victimMaxHealth, double fraction = AssistFraction)
  {
    if (victimMaxHealth <= 0)
      return [];

    var needed = fraction * victimMaxHealth;

    return DamageTo(victimId)
      .Where(entry => entry.Key != killerId && entry.Value >= needed)
      .OrderByDescending(entry => entry.Value)
      .Select(entry => entry.Key)
      .ToList();
  }

  public void Clear(ulong victimId) => _byVictim.Remove(victimId);

  public void Forget(ulong steamId)
  {
    _byVictim.Remove(steamId);

    foreach (var attackers in _byVictim.Values)
      attackers.Remove(steamId);
  }

  public void Reset() => _byVictim.Clear();
}
