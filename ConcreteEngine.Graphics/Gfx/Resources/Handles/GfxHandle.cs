#region

#endregion

#region

using System.Runtime.Serialization;
using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Resources;

internal readonly record struct GfxRefToken<TId>(in GfxHandle Handle)
    where TId : unmanaged, IResourceId
{
    public int Slot => Handle.Slot;
    public ushort Gen => Handle.Gen;
    public bool IsValid => Handle.Gen > 0 && Handle.Kind != ResourceKind.Invalid;

    public static GfxRefToken<TId> From(in GfxHandle handle) => new(in handle);
    public static GfxRefToken<TId> Make(int slot, ushort gen) => new(new GfxHandle(slot, gen, TId.Kind));

    public static explicit operator GfxRefToken<TId>(GfxHandle handle) => new(handle);
    public static implicit operator GfxHandle(GfxRefToken<TId> typed) => typed.Handle;

    public override string ToString() => Handle.ToString();
}

internal readonly record struct GfxHandle(int Slot, ushort Gen, ResourceKind Kind)
{
    [IgnoreDataMember] public bool IsValid => Gen > 0 && Kind != ResourceKind.Invalid;

    public string ToDebugString() => $"Gen={Gen,-2} Slot={Slot,-2}";
}

internal readonly record struct BkHandle<THandle>(THandle Handle, bool Alive)
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
{
    public static implicit operator uint(BkHandle<THandle> typed) => typed.Handle.Value;
    public static implicit operator THandle(BkHandle<THandle> typed) => typed.Handle;

    [IgnoreDataMember] public bool IsValid => Handle.Value > 0 && Alive;
}