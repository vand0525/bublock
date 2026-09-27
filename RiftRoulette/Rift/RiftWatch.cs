namespace RiftRoulette.Rift;

public enum RiftObservation
{
  Waiting,
  CashinAppeared,
  Finished,
  Tied
}

public sealed class RiftWatch
{
  public bool SawCashin { get; private set; }

  public RiftObservation Observe(bool newTrooper, bool cashinExists)
  {
    if (newTrooper)
      return RiftObservation.Finished;

    if (cashinExists)
    {
      var first = !SawCashin;
      SawCashin = true;
      return first ? RiftObservation.CashinAppeared : RiftObservation.Waiting;
    }

    return SawCashin ? RiftObservation.Tied : RiftObservation.Waiting;
  }
}
