using System.Runtime.Serialization;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Graphics.Gfx.Resources;

internal readonly record struct GfxRefToken<TId>(int Slot, ushort Gen) where TId : unmanaged, IResourceId
{
    public readonly int Slot = Slot;
    public readonly ushort Gen = Gen;
    public readonly ResourceKind Kind = TId.Kind;

    public bool IsValid => Gen > 0 && Kind != ResourceKind.Invalid;

    public static implicit operator GfxRefToken<TId>(GfxHandle handle) => new(handle.Slot, handle.Gen);
    public static implicit operator GfxHandle(GfxRefToken<TId> typed) => new(typed.Slot, typed.Gen, typed.Kind);
}

internal readonly record struct GfxHandle(int Slot, ushort Gen, ResourceKind Kind)
{
    public readonly int Slot = Slot;
    public readonly ushort Gen = Gen;
    public readonly ResourceKind Kind = Kind;
    [IgnoreDataMember] public bool IsValid => Gen > 0 && Kind != ResourceKind.Invalid;
}

internal readonly record struct BkHandle(uint Handle, bool Alive)
{
    public readonly uint Handle = Handle;
    public readonly bool Alive = Alive;

    public static implicit operator uint(BkHandle typed) => typed.Handle;

    [IgnoreDataMember] public bool IsValid => Handle > 0 && Alive;
}