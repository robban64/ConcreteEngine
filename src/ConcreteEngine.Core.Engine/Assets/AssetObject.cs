using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Assets;

public abstract class AssetObject(string name) : IComparable<AssetObject>
{
    private IAssetChangeNotifier? _changeNotifier;
    
    [InspectablePrimitive(FieldKind = InspectorFieldKind.Id)]
    public required AssetId Id { get; init; }

    [Inspectable]
    public required Guid GId { get; init; } = Guid.NewGuid();

    [Inspectable(FieldKind = InspectorFieldKind.Name)]
    public string Name
    {
        get;
        private set
        {
            if (field == value) return;
            field = value;

            if(value.Length > 0)
                PackedName = StringPacker.PackUtf8(value.AsSpan());
        }
    }  = name;

    public ulong PackedName { get; private set; }


    [Inspectable(FieldKind = InspectorFieldKind.Generation)]
    public int Generation { get; init; } = 1;

    public bool IsCoreAsset { get; init; }
    
    public abstract AssetCategory Category { get; }
    public abstract AssetKind Kind { get; }
    internal abstract AssetObject CopyAndIncreaseGen();

    public void SetName(string newName)
    {
        if(_changeNotifier is not {} changeNotifier) return;
        changeNotifier.Rename(this, newName, (newName) => Name = newName);
    }
    protected void MarkDirty()
    {
        _changeNotifier?.MarkDirty(this);
    }
    
    internal void AttachNotifier(IAssetChangeNotifier changeNotifier)
    {
        if(string.IsNullOrWhiteSpace(Name)) throw new InvalidOperationException("Name is null or empty");
        _changeNotifier = changeNotifier;
    }
    
    public int CompareTo(AssetObject? other)
    {
        return other is null ? 1 : Id.Value.CompareTo(other.Id.Value);
    }
}