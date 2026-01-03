using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Utils;

internal static class AssetEnums
{
    public static AssetKind ToAssetKind<T>() where T : AssetObject
    {
        if (typeof(T) == typeof(Shader)) return AssetKind.Shader;
        if (typeof(T) == typeof(Model)) return AssetKind.Model;
        if (typeof(T) == typeof(Texture2D)) return AssetKind.Texture2D;
        if (typeof(T) == typeof(CubeMap)) return AssetKind.TextureCubeMap;
        if (typeof(T) == typeof(MaterialTemplate)) return AssetKind.MaterialTemplate;
        
        throw new ArgumentOutOfRangeException(nameof(T));
    }
}