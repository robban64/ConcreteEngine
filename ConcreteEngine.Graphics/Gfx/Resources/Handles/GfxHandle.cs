#region

#endregion

using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Graphics.Gfx.Resources;

internal readonly record struct GfxRefToken<TId>(in GfxHandle Handle)
    where TId : unmanaged, IResourceId
{
    public static GfxRefToken<TId> From(in GfxHandle handle) => new(in handle);
    
    public static explicit operator GfxRefToken<TId>(GfxHandle handle) => new(handle);
    public static implicit operator GfxHandle(GfxRefToken<TId> typed) => typed.Handle;

}

internal readonly record struct GfxHandle(int Slot, ushort Gen, ResourceKind Kind)
{
    public bool IsValid { get; } = Gen > 0 && Kind != ResourceKind.Invalid;
}

internal readonly record struct BkHandle<THandle>(THandle Handle, ushort Gen, bool Alive)
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
{
    public bool IsValid { get; } = Handle.Value > 0 && Gen > 0 && Alive;
}