using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Core.Engine.Assets;


public abstract class AssetObject : IComparable<AssetObject>
{
    public const int MaxNameLength = 64;

    private readonly List<AssetRef> _listeners = [];
    public AssetDirtyFlag DirtyFlags { get; private set; }
    public AssetId Id { get; }
    public Guid GId { get; }

    public string Name
    {
        get;
        internal set
        {
            if (field == value) return;
            field = value.Length > MaxNameLength ? value.Substring(0, MaxNameLength) : value;
            PackedName = StringPacker.PackAscii(field, true);
        }
    }

    public ulong PackedName { get; private set; }

    protected AssetObject(string name, AssetId id, Guid gId)
    {
        Name = name;
        Id = id;
        GId = gId;
    }

    public abstract AssetCategory Category { get; }
    public abstract AssetKind Kind { get; }

    public bool Rename(string newName)
    {
        AssetManager.Assets.Rename(this, newName);
        Name = newName;
        return true;
    }

    protected internal void MarkDirty(AssetDirtyFlag flag)
    {
        if (!Id.IsValid() || (DirtyFlags & flag) != 0) return;
        DirtyFlags |= flag;
        AssetManager.Assets.MarkDirty(this);
    }

    internal AssetDirtyFlag Commit()
    {
        var f = DirtyFlags;
        var shouldTrigger = (f & AssetDirtyFlag.Structure) != 0 ||
                            (f & AssetDirtyFlag.Dependencies) != 0 ||
                            (f & AssetDirtyFlag.Lifecycle) != 0;
        if (shouldTrigger)
        {
            OnCommit();
            foreach (var it in _listeners)
                it.Trigger();
        }

        DirtyFlags = 0;
        return f;
    }
    protected virtual void OnCommit(){}

    public void AddRef(AssetRef assetRef) => _listeners.Add(assetRef);
    public void RemoveRef(AssetRef assetRef) => _listeners.Remove(assetRef);

    public int CompareTo(AssetObject? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Id.Value.CompareTo(other.Id.Value);
    }
}