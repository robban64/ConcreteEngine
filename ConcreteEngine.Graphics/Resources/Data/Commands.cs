namespace ConcreteEngine.Graphics.Resources;

internal readonly record struct DeleteResourceCommand(
    in GfxHandle Handle,
    NativeHandle NativeHandle,
    int IdValue,
    ushort Priority,
    bool Replace
);