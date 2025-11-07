namespace ConcreteEngine.Graphics.Gfx.Resources;

internal readonly record struct DeleteResourceCommand(
    GfxHandle Handle,
    NativeHandle BackendHandle,
    int GfxId,
    ushort Priority,
    bool Replace)
{
    public static DeleteResourceCommand MakeReplace(GfxHandle gfxHandle, NativeHandle bkHandle, ushort priority = 0) =>
        new(gfxHandle, bkHandle, 0, priority, true);

    public static DeleteResourceCommand MakeDelete(GfxHandle gfxHandle, NativeHandle bkHandle, int gfxId,
        ushort priority = 0) =>
        new(gfxHandle, bkHandle, gfxId, priority, false);
}