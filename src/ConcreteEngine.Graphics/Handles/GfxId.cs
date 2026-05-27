using System.Runtime.CompilerServices;

namespace ConcreteEngine.Graphics.Handles;

public readonly record struct GfxId<TMeta>(ushort Id) : IComparable<GfxId<TMeta>> where TMeta : unmanaged, IResourceMeta
{
    public GfxId(int id) : this((ushort)id){}
    
    public readonly ushort Id = Id;
    public int Value => Id;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Id > 0;

    public static implicit operator ushort(GfxId<TMeta> id) => id.Id;
    public static explicit operator GfxId<TMeta>(int value) => new(value);
    public int CompareTo(GfxId<TMeta> other) => Id.CompareTo(other.Id);
}


/*
public readonly record struct GfxId(int ResourceId, ushort Gen, GraphicsKind Kind)
{
    public readonly int ResourceId = ResourceId;
    public readonly ushort Gen = Gen;
    public readonly GraphicsKind Kind = Kind;
    public bool IsValid() => ResourceId > 0 && Gen > 0 && Kind != GraphicsKind.Invalid;

    internal bool ValidateHandle(GfxHandle handle) => Gen == handle.Gen && Kind == handle.Kind;
}
*/
