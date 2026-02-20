using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Controller.Proxy;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Editor.Controller;

public abstract class AssetController
{
    public abstract string GetAssetName(AssetId id);
    public abstract TextureId GetTextureId(AssetId id, out TextureKind kind);
    public abstract int FilterQuery(in SearchPayload<AssetId> search, SearchAssetFilter filter, SearchAssetDel del);
    public abstract AssetFileSpec[] FetchAssetFileSpecs(AssetId assetId);
    public abstract AssetObjectProxy GetAssetProxy(AssetId assetId);
}

public readonly struct AssetQueryItem(string name, ulong nameKey, ushort generation, AssetKind kind)
{
    public readonly string Name = name;
    public readonly ulong NameKey = nameKey;
    public readonly ushort Generation = generation;
    public readonly AssetKind Kind = kind;
}