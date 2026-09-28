using System.Text.Json;
using Bublock.Shared;
using RiftRoulette.Rift;

namespace RiftRoulette.Lobby;

public static class StreamFramingStore
{
  public const string FileName = "streamcam.json";

  private static readonly Logger Log = BublockLog.For("Lobby");

  private static Dictionary<RiftSide, CameraPose>? _poses;

  public static string FilePath =>
    Path.Combine(Path.GetDirectoryName(LogPaths.ResolveRoot())!, FileName);

  public static IReadOnlyDictionary<RiftSide, CameraPose> All => _poses ??= Load();

  public static CameraPose For(RiftSide side) => StreamFraming.Pick(All, side);

  public static void Save(RiftSide side, CameraPose pose, ExecutionMode mode = ExecutionMode.Clean)
  {
    var poses = new Dictionary<RiftSide, CameraPose>(All) { [side] = pose };
    _poses = poses;
    Write(poses, mode);
  }

  public static void Reset(ExecutionMode mode = ExecutionMode.Clean)
  {
    _poses = [];
    Write(_poses, mode);
  }

  private static Dictionary<RiftSide, CameraPose> Load()
  {
    var path = FilePath;

    try
    {
      if (!File.Exists(path))
        return [];

      var poses = StreamFraming.Parse(File.ReadAllText(path));
      Log.Info("Stream camera framing loaded Sides={Sides}", string.Join(",", poses.Keys.Select(RiftSides.Name)));
      return poses;
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
    {
      Log.Error(exception, "Stream camera framing unreadable, using the default Path={Path}", path);
      return [];
    }
  }

  private static void Write(Dictionary<RiftSide, CameraPose> poses, ExecutionMode mode)
  {
    var path = FilePath;

    try
    {
      File.WriteAllText(path, StreamFraming.Serialize(poses));
      Log.WithMode(mode).Debug("Stream camera framing written Path={Path} Sides={Sides}", path, poses.Count);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      Log.Error(exception, "Stream camera framing not written, kept for this load only Path={Path}", path);
    }
  }
}
