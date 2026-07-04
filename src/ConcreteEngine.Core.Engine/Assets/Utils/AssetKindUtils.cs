using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Engine.Assets.Utils;

internal static class AssetKindUtils
{
    public static readonly int AssetTypeCount = 4;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AssetKind ToAssetKind(Type type)
    {
        if (type == typeof(Shader)) return AssetKind.Shader;
        if (type == typeof(Model)) return AssetKind.Model;
        if (type == typeof(Texture)) return AssetKind.Texture;
        if (type == typeof(Material)) return AssetKind.Material;

        throw new ArgumentOutOfRangeException(nameof(type));
    }

    public static Type ToType(AssetKind kind)
    {
        return kind switch
        {
            AssetKind.Shader => typeof(Shader),
            AssetKind.Model => typeof(Model),
            AssetKind.Texture => typeof(Texture),
            AssetKind.Material => typeof(Material),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}