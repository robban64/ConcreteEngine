namespace ConcreteEngine.Core.Engine.Assets;


public interface IAsset
{
    AssetId Id { get;  }
    Guid GId { get;  }
    string Name { get;  }
    bool IsCoreAsset { get;  }
    int Generation { get;  }

    AssetCategory Category { get; }
    AssetKind Kind { get; }

}

public abstract record AssetObject : IAsset, IComparable<AssetObject>
{
    public required AssetId Id { get; init; }
    public required Guid GId { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public bool IsCoreAsset { get; init; }
    public int Generation { get; init; } = 1;

    public abstract AssetCategory Category { get; }
    public abstract AssetKind Kind { get; }

    public int CompareTo(AssetObject? other)
    {
        return other is null ? 1 : Id.Value.CompareTo(other.Id.Value);
    }
}