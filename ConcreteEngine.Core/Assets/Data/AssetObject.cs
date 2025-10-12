using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Core.Assets;

public abstract record AssetObject(
    AssetId Id,
    string Name,
    AssetKind Kind,
    AssetCategory Category,
    bool IsCoreAsset,
    int Generation
);

public sealed record DataAssetObject(
    AssetId Id,
    string Name,
    AssetKind Kind,
    AssetCategory Category,
    bool IsCoreAsset,
    int Generation
) : AssetObject(Id, Name, Kind, Category, IsCoreAsset, Generation);

public sealed record GraphicAssetObject<TId>(
    AssetId Id,
    string Name,
    AssetKind Kind,
    AssetCategory Category,
    bool IsCoreAsset,
    int Generation
) : AssetObject(Id, Name, Kind, Category, IsCoreAsset, Generation)
    where TId : unmanaged, IResourceId
{
    public required TId ResourceId { get; init; }
}