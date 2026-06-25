using System.Runtime.CompilerServices;

namespace ConcreteEngine.Graphics.Handles;

public readonly record struct GfxId<TMeta>(ushort Id) : IComparable<GfxId<TMeta>> where TMeta : unmanaged, IResourceMeta
{
    public GfxId(int id) : this((ushort)id) { }

    public readonly ushort Id = Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Id > 0;

    public static implicit operator ushort(GfxId<TMeta> id) => id.Id;
    public static explicit operator GfxId<TMeta>(int value) => new(value);
    public int CompareTo(GfxId<TMeta> other) => Id.CompareTo(other.Id);
}

internal readonly record struct GfxId(ushort Id, GraphicsKind Kind)
{
    public static implicit operator ushort(GfxId id) => id.Id;

    public bool IsValid() => Id > 0 && Kind != GraphicsKind.Invalid;
}