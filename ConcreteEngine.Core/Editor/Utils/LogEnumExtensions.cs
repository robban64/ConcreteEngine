using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Core.Assets.Data;

namespace ConcreteEngine.Core.Editor.Utils;

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
            AssetKind.Material => LogTopic.Material,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}