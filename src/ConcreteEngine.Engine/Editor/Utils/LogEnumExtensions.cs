using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Engine.Editor.Utils;

internal static class LogEnumExtensions
{
    public static LogTopic ToLogTopic(this AssetKind kind)
    {
        return kind switch
        {
            AssetKind.Unknown => LogTopic.Unknown,
            AssetKind.Shader => LogTopic.Shader,
            AssetKind.Model => LogTopic.Mesh,
            AssetKind.Texture => LogTopic.Texture,
            AssetKind.Material => LogTopic.Material,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}