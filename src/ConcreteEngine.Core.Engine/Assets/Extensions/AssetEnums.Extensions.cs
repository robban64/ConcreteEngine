using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Configuration;

namespace ConcreteEngine.Core.Engine.Assets.Extensions;

public static class AssetEnumsExtensions
{
    extension(AssetKind kind)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToText()
        {
            return kind switch
            {
                AssetKind.Unknown => "Unknown",
                AssetKind.Shader => "Shader",
                AssetKind.Model => "Model",
                AssetKind.Texture => "Texture",
                AssetKind.Material => "Material",
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToRootFolder()
        {
            return kind switch
            {
                AssetKind.Shader => EnginePath.ShaderFolder,
                AssetKind.Model => EnginePath.MeshFolder,
                AssetKind.Texture => EnginePath.TextureFolder,
                AssetKind.Material => EnginePath.MaterialFolder,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }
    }
}