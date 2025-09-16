namespace ConcreteEngine.Graphics.Resources;

internal readonly record struct DeleteCmd(
    in GfxHandle Handle,
    NativeHandle NativeHandle,
    int IdValue,
    ushort Priority,
    bool Replace
);