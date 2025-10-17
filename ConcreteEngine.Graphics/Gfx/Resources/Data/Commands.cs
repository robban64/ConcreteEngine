namespace ConcreteEngine.Graphics.Gfx.Resources;

internal readonly struct DeleteResourceCommand(
    in GfxHandle handle,
    NativeHandle nativeHandle,
    int idValue,
    ushort priority,
    bool replace
)
{
    public GfxHandle Handle { get; init; } = handle;
    public NativeHandle NativeHandle { get; init; } = nativeHandle;
    public int IdValue { get; init; } = idValue;
    public ushort Priority { get; init; } = priority;
    public bool Replace { get; init; } = replace;

}