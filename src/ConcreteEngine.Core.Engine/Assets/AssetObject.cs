using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Core.Engine.Assets;

public abstract class AssetObject : IComparable<AssetObject>
{
    public const int MaxNameLength = 64;

    private readonly List<IAssetListener> _listeners = [];

    public AssetDirtyFlag DirtyFlags { get; private set; }
    public AssetId Id { get; }
    public Guid GId { get; }

    public string Name
    {
        get;
        internal set
        {
            if (field == value) return;
            if (!string.IsNullOrEmpty(field)) MarkDirty(AssetDirtyFlag.Name);
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

    public bool SetName(string newName)
    {
        AssetManager.Instance.Rename(this, newName);
        Name = newName;
        MarkDirty(AssetDirtyFlag.Name);
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
        var shouldTrigger = (f & AssetDirtyFlag.Structure) != 0 || (f & AssetDirtyFlag.Dependencies) != 0 ||
                            (f & AssetDirtyFlag.Lifecycle) != 0;
        if (shouldTrigger)
        {
            OnCommit();
            foreach (var it in _listeners)
                it.OnAssetChanged(this);
        }

        DirtyFlags = 0;
        return f;
    }

    protected virtual void OnCommit() { }

    public void AddRef(IAssetListener listener) => _listeners.Add(listener);
    public void RemoveRef(IAssetListener listener) => _listeners.Remove(listener);

    public int CompareTo(AssetObject? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Id.Id.CompareTo(other.Id.Id);
    }
}