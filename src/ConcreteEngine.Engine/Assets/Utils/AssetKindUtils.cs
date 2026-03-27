using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Engine.Assets.Utils;

internal static class AssetKindUtils
{
    public static readonly int AssetTypeCount = EnumCache<AssetKind>.Count - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToIndex(AssetKind kind) => (int)kind - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToAssetIndex(Type type)  => ToIndex(ToAssetKind(type));

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