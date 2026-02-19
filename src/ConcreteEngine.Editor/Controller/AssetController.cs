using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Controller.Proxy;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Editor.Controller;

public abstract class AssetController
{
    public abstract string GetAssetName(AssetId id);
    public abstract TextureId GetTextureId(AssetId id);
    public abstract int FilterQuery(in SearchPayload<AssetId> search, SearchFilter filter, SearchAssetDel del);
    public abstract AssetFileSpec[] FetchAssetFileSpecs(AssetId assetId);
    public abstract AssetObjectProxy GetAssetProxy(AssetId assetId);
}

public readonly struct TextureListItem(string name, TextureId texture)
{
    public readonly string Name = name;
    public readonly TextureId Texture = texture;
}

public readonly struct AssetQueryItem(string name, ulong nameKey, ushort generation, AssetKind kind)
{
    public readonly string Name = name;
    public readonly ulong NameKey = nameKey;
    public readonly ushort Generation = generation;
    public readonly AssetKind Kind = kind;
}