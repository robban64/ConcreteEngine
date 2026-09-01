using System.Runtime.CompilerServices;

namespace ConcreteEngine.Graphics.Gfx;

public readonly record struct GfxId<TMeta> : IComparable<GfxId<TMeta>> where TMeta : unmanaged, IResourceMeta
{
    public readonly ushort Id;
    
    public GfxId(ushort id)
    {
        Id = id;
    }

    public GfxId(int id)
    {
        Id = (ushort)id;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int Index() => Id - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Id > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ushort(GfxId<TMeta> id) => id.Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator GfxId<TMeta>(int value) => new(value);

    public int CompareTo(GfxId<TMeta> other) => Id.CompareTo(other.Id);
}