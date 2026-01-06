using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Engine.Metadata.Asset;

namespace ConcreteEngine.Engine.Assets;


public readonly record struct AssetRef<TAsset>(AssetId Id) where TAsset : AssetObject
{
    public bool IsValid() => Id.IsValid();

    public static implicit operator int(AssetRef<TAsset> typed) => typed.Id;
    public static implicit operator AssetId(AssetRef<TAsset> typed) => typed.Id;

    public static AssetRef<TAsset> Make(AssetId id) => new(id);
}

public readonly record struct ReservedAsset(AssetId Asset, AssetKind Kind, long ReservedAt);