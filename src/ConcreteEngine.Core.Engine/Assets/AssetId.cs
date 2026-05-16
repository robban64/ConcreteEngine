using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;

namespace ConcreteEngine.Core.Engine.Assets;


public readonly record struct AssetId(ushort Value, ushort Gen) : IComparable<AssetId>
{
    public AssetId(int id, int gen): this((ushort)id, (ushort)gen){}
    public readonly ushort Value = Value;
    public readonly ushort Gen = Gen;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Value > 0 && Gen > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Value - 1;

    public static implicit operator Handle<AssetObject>(AssetId handle) => new(handle.Value,handle.Gen);
    public static explicit operator AssetId(Handle<AssetObject> handle) => new(handle.Value,handle.Gen);
    public static explicit operator int(AssetId handle) => handle.Value;

    public int CompareTo(AssetId other) => Value.CompareTo(other.Value);

    public static AssetId Empty = new(0, 0);
}

public readonly record struct AssetIndexRef(Guid AssetGId, int Index);