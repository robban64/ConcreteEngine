using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Core.Engine.Assets;

public abstract class AssetObject : IComparable<AssetObject>
{
    public const int MaxNameLength = 64;
    
    public bool IsDirty { get; private set; }
    public AssetId Id { get;  }
    public Guid GId { get;  } 

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
        AssetStore.Instance.Rename(this, newName);
        Name = newName;
        return true;
    }
    
    protected internal void MarkDirty()
    {
        if(!Id.IsValid()) return;
        if(IsDirty) return;
        IsDirty = true;
        AssetStore.Instance.MarkDirty(this);
    }
    
    internal void ClearDirty() => IsDirty = false;

    public int CompareTo(AssetObject? other)
    {
        if(ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Id.Value.CompareTo(other.Id.Value);
    }
}