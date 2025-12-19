using System.Runtime.Serialization;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Graphics.Gfx.Resources.Handles;

internal readonly struct GfxRefToken<TId>(GfxHandle handle) where TId : unmanaged, IResourceId
{
    public readonly GfxHandle Handle = handle;

    public int Slot => Handle.Slot;
    public ushort Gen => Handle.Gen;

    public bool IsValid => Handle.Gen > 0 && Handle.Kind != ResourceKind.Invalid;

    public static GfxRefToken<TId> Make(int slot, ushort gen) => new(new GfxHandle(slot, gen, TId.Kind));

    public static explicit operator GfxRefToken<TId>(GfxHandle handle) => new(handle);
    public static implicit operator GfxHandle(GfxRefToken<TId> typed) => typed.Handle;

    public override string ToString() => Handle.ToString();
}

internal readonly record struct GfxHandle(int Slot, ushort Gen, ResourceKind Kind)
{
    [IgnoreDataMember] public bool IsValid => Gen > 0 && Kind != ResourceKind.Invalid;
}

internal readonly record struct BkHandle(uint Handle, bool Alive)
{
    public readonly uint Handle = Handle;
    public readonly bool Alive = Alive;

    public static implicit operator uint(BkHandle typed) => typed.Handle;

    [IgnoreDataMember] public bool IsValid => Handle > 0 && Alive;
}