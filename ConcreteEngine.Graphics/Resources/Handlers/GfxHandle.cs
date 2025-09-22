#region

using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Graphics.Resources;

public readonly record struct GfxHandle(uint Slot, ushort Gen, ResourceKind Kind)
{
    public readonly bool IsValid = Gen > 0 && Kind != ResourceKind.Invalid;
}

internal readonly struct GfxRefToken<TId>(in GfxHandle handle)
    where TId : unmanaged, IResourceId
{
    public readonly GfxHandle Handle = handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxRefToken<TId> From(in GfxHandle handle) => new(in handle);
}