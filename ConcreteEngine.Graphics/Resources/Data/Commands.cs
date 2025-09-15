namespace ConcreteEngine.Graphics.Resources;

internal readonly record struct RecreateCmd(int? RawId, uint Slot, uint Gen);
internal readonly record struct CreateCmd(int SubmitIdx, uint RawHandle, RecreateCmd? Replace, ResourceKind Kind);
internal readonly record struct DeleteCmd(
    in GfxHandle Handle,
    int IdValue,
    uint RawHandle,
    ushort Priority,
    bool Replace
);