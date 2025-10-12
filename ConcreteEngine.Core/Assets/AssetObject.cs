using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Core.Assets;

public abstract class AssetObject
{
    public required AssetId Id { get; init; }
    public required string Name { get; init; }
    public required bool IsCoreAsset { get; init; }
    public required int Generation { get; init; }
    
    public abstract AssetKind Kind { get; }
    public abstract AssetCategory Category { get; }
    
    internal AssetObject(){}

}