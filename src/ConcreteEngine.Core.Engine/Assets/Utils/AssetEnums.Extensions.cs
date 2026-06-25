using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Configuration;

namespace ConcreteEngine.Core.Engine.Assets.Utils;

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
    }
}