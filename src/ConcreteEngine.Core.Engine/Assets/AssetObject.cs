using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Assets;

public abstract class AssetObject : IComparable<AssetObject>
{
    public const int MaxNameLength = 64;

    private IAssetChangeNotifier? _changeNotifier;

    [InspectablePrimitive(FieldKind = InspectorFieldKind.Id)]
    public required AssetId Id { get; init; }

    [Inspectable]
    public required Guid GId { get; init; } = Guid.NewGuid();

    [Inspectable(FieldKind = InspectorFieldKind.Name)]
    public string Name
    {
        get;
        internal set
        {
            if (field == value) return;
            field = value;
            PackedName = StringPacker.PackAscii(value.AsSpan(), true);
        }
    }

    public ulong PackedName { get; private set; }

    [Inspectable(FieldKind = InspectorFieldKind.Generation)]
    public int Generation { get; init; } = 1;

    public bool IsCoreAsset { get; init; }

    protected AssetObject(string name)
    {
        Name = name;
    }

    public abstract AssetCategory Category { get; }
    public abstract AssetKind Kind { get; }
    internal abstract AssetObject CopyAndIncreaseGen();

    public bool Rename(string newName)
    {
        if (_changeNotifier is not { } changeNotifier)
            throw new InvalidOperationException(nameof(_changeNotifier));

        changeNotifier.Rename(this, newName);
        Name = newName;
        return true;
    }

    protected void MarkDirty()
    {
        _changeNotifier?.MarkDirty(this);
    }

    internal void AttachNotifier(IAssetChangeNotifier changeNotifier)
    {
        if (string.IsNullOrWhiteSpace(Name)) throw new InvalidOperationException("Name is null or empty");
        _changeNotifier = changeNotifier;
    }

    public int CompareTo(AssetObject? other)
    {
        return other is null ? 1 : Id.Value.CompareTo(other.Id.Value);
    }
}