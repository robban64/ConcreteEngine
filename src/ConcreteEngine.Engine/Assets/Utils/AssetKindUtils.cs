using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Engine.Assets.Utils;

internal static class AssetKindUtils
{
    public static readonly int AssetTypeCount = EnumCache<AssetKind>.Count - 1;

    public static int ToAssetIndex(AssetKind kind) => (int)kind - 1;

    public static int ToAssetIndex<T>() where T : AssetObject => (int)ToAssetKind<T>() - 1;

    public static AssetKind ToAssetKind<T>() where T : AssetObject
    {
        if (typeof(T) == typeof(Shader)) return AssetKind.Shader;
        if (typeof(T) == typeof(Model)) return AssetKind.Model;
        if (typeof(T) == typeof(Texture)) return AssetKind.Texture;
        if (typeof(T) == typeof(Material)) return AssetKind.Material;

        throw new ArgumentOutOfRangeException(nameof(T));
    }
}