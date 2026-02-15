using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Engine.Assets;

public abstract class AssetObject : IAsset, IComparable<AssetObject>
{
    [InspectablePrimitive]
    public required AssetId Id { get; init; }
    [Inspectable]
    public required Guid GId { get; init; } = Guid.NewGuid();

    [Inspectable]
    public required string Name
    {
        get;
        init
        {
            if (field == value) return;
            field = value;
            PackedName = StringPacker.PackUtf8(value.AsSpan());
        }
    }
    internal ulong PackedName { get; private set; }

    public bool IsCoreAsset { get; init; }
    
    [Inspectable]
    public int Generation { get; init; } = 1;
    
    public abstract AssetCategory Category { get; }
    public abstract AssetKind Kind { get; }
    internal abstract AssetObject CopyAndIncreaseGen();

    public int CompareTo(AssetObject? other)
    {
        return other is null ? 1 : Id.Value.CompareTo(other.Id.Value);
    }
}