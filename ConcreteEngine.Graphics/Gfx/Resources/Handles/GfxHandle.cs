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
    public static GfxRefToken<TId> From(in GfxHandle handle) => new(in handle);

    public static explicit operator GfxRefToken<TId>(GfxHandle handle) => new(handle);
    public static implicit operator GfxHandle(GfxRefToken<TId> typed) => typed.Handle;

    public override string ToString() => Handle.ToString();
}

internal readonly record struct GfxHandle(int Slot, ushort Gen, ResourceKind Kind)
{
    [IgnoreDataMember] public bool IsValid => Gen > 0 && Kind != ResourceKind.Invalid;

    public string ToDebugString() => $"Gen={Gen,-2} Slot={Slot,-2}";
}

internal readonly record struct BkHandle<THandle>(THandle Handle, ushort Gen, bool Alive)
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
{
    [IgnoreDataMember] public bool IsValid => Handle.Value > 0 && Gen > 0 && Alive;
}