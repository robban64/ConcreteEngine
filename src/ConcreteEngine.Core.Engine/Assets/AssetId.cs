using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Identity;

namespace ConcreteEngine.Core.Engine.Assets;

public readonly record struct AssetId(int Value) : IComparable<AssetId>
{
    public bool IsValid() => Value > 0;
    public int Index() => Value - 1;
    
    public int CompareTo(AssetId other) => Value.CompareTo(other.Value);

    public static implicit operator int(AssetId id) => id.Value;
    public static implicit operator Id32<AssetObject>(AssetId id) => new(id.Value);
    public static implicit operator AssetId(Id32<AssetObject> id) => new(id.Value);

    public static AssetId Empty = new(0);
}

public readonly record struct AssetFileId(int Value) : IComparable<AssetFileId>
{
    public bool IsValid() => Value > 0;
    public int Index() => Value - 1;
    public int CompareTo(AssetFileId other) => Value.CompareTo(other.Value);

    public static implicit operator int(AssetFileId id) => id.Value;
    public static implicit operator Id32<AssetFileSpec>(AssetFileId id) => new(id.Value);

    public static AssetId Empty = new(0);
}

public readonly record struct AssetIndexRef(Guid AssetGId, int Index);