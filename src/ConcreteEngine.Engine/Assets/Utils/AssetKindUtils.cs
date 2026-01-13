using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Engine.Assets.Utils;

internal static class AssetKindUtils
{
    public static int AssetTypeCount = EnumCache<AssetKind>.Count - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToAssetIndex(AssetKind kind) => (int)kind - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToAssetIndex<T>() where T : AssetObject => (int)ToAssetKind<T>() - 1;

    public static AssetKind ToAssetKind<T>() where T : AssetObject
    {
        if (typeof(T) == typeof(Shader)) return AssetKind.Shader;
        if (typeof(T) == typeof(Model)) return AssetKind.Model;
        if (typeof(T) == typeof(Texture)) return AssetKind.Texture;
        if (typeof(T) == typeof(MaterialTemplate)) return AssetKind.Material;

        throw new ArgumentOutOfRangeException(nameof(T));
    }
}