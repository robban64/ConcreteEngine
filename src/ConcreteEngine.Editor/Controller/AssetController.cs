using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Controller.Proxy;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Controller;

public abstract class AssetController
{
    public abstract void GetAssetItem(AssetId id, out AssetItem result);
    public abstract int FilterQuery(in SearchPayload<AssetId> search, SearchFilter filter, SearchAssetDel del);
    public abstract AssetFileSpec[] FetchAssetFileSpecs(AssetId assetId);
    public abstract AssetObjectProxy GetAssetProxy(AssetId assetId);
}

public readonly struct AssetItem(string name, ulong nameKey, ushort generation, AssetKind kind)
{
    public readonly string Name = name;
    public readonly ulong NameKey = nameKey;
    public readonly ushort Generation = generation;
    public readonly AssetKind Kind = kind;
}