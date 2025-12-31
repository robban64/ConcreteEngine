using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Engine.Metadata;

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
            AssetKind.Texture2D => LogTopic.Texture,
            AssetKind.TextureCubeMap => LogTopic.Texture,
            AssetKind.MaterialTemplate => LogTopic.Material,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}