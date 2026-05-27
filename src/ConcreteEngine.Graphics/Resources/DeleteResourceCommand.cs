using ConcreteEngine.Graphics.Handles;

namespace ConcreteEngine.Graphics.Resources;

internal readonly struct DeleteResourceCommand(
    GfxHandle handle,
    NativeHandle backendHandle,
    ushort gfxId,
    bool replace) : IEquatable<DeleteResourceCommand>
{
    public readonly NativeHandle BackendHandle = backendHandle;
    public readonly GfxHandle Handle = handle;
    public readonly ushort GfxId = gfxId;
    public readonly bool Replace = replace;

    public static DeleteResourceCommand MakeReplace(GfxHandle gfxHandle, NativeHandle bkHandle) =>
        new(gfxHandle, bkHandle, 0, true);

    public static DeleteResourceCommand MakeDelete(GfxHandle gfxHandle, NativeHandle bkHandle, ushort gfxId) =>
        new(gfxHandle, bkHandle, gfxId, false);


    public bool Equals(DeleteResourceCommand other) => Handle.Equals(other.Handle);
    public override bool Equals(object? obj) => obj is DeleteResourceCommand other && Equals(other);
    public override int GetHashCode() => Handle.GetHashCode();
}