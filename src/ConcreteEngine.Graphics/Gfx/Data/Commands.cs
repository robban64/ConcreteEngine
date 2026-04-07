using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Graphics.Gfx.Data;

internal sealed class DeleteResourceCommand(
    GfxHandle handle,
    NativeHandle backendHandle,
    int gfxId,
    ushort priority,
    bool replace)
{
    public readonly GfxHandle Handle = handle;
    public readonly NativeHandle BackendHandle = backendHandle;
    public readonly int GfxId = gfxId;
    public readonly ushort Priority = priority;
    public readonly bool Replace = replace;

    public static DeleteResourceCommand MakeReplace(GfxHandle gfxHandle, NativeHandle bkHandle, ushort priority = 0) =>
        new(gfxHandle, bkHandle, 0, priority, true);

    public static DeleteResourceCommand MakeDelete(GfxHandle gfxHandle, NativeHandle bkHandle, int gfxId,
        ushort priority = 0) =>
        new(gfxHandle, bkHandle, gfxId, priority, false);
}