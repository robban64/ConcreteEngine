using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Assets;

public abstract class AssetObject : IComparable<AssetObject>
{
    private IAssetChangeNotifier? _changeNotifier;
    
    [InspectablePrimitive(FieldKind = InspectorFieldKind.Id)]
    public required AssetId Id { get; init; }

    [Inspectable]
    public required Guid GId { get; init; } = Guid.NewGuid();

    [Inspectable(FieldKind = InspectorFieldKind.Name)]
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

    public ulong PackedName { get; private set; }

    public bool IsCoreAsset { get; init; }

    [Inspectable(FieldKind = InspectorFieldKind.Generation)]
    public int Generation { get; init; } = 1;

    public abstract AssetCategory Category { get; }
    public abstract AssetKind Kind { get; }
    internal abstract AssetObject CopyAndIncreaseGen();

    protected void MarkDirty()
    {
        _changeNotifier?.MarkDirty(this);
    }
    
    internal void AttachNotifier(IAssetChangeNotifier changeNotifier)
    {
        _changeNotifier = changeNotifier;
    }
    public int CompareTo(AssetObject? other)
    {
        return other is null ? 1 : Id.Value.CompareTo(other.Id.Value);
    }
}