using System.Numerics;

namespace Bublock.Modules.WorldText;

public sealed record WorldTextSpec(
  string Text,
  Vector3 Position,
  Vector3 Angle,
  WorldTextColor Color,
  float WorldUnitsPerPx,
  float FontSize = 64f,
  int ReorientMode = 0);
