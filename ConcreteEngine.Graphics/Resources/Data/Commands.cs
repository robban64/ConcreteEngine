namespace ConcreteEngine.Graphics.Resources;

internal readonly record struct DeleteCmd(
    in GfxHandle Handle,
    int IdValue,
    uint RawHandle,
    ushort Priority,
    bool Replace
);