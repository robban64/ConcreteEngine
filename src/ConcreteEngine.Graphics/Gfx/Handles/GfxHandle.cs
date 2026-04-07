namespace ConcreteEngine.Graphics.Gfx.Handles;

internal readonly record struct GfxHandle(int Slot, ushort Gen, GraphicsKind Kind)
{
    public readonly int Slot = Slot;
    public readonly ushort Gen = Gen;
    public readonly GraphicsKind Kind = Kind;
    public bool IsValid => Gen > 0 && Kind != GraphicsKind.Invalid;
}

internal readonly record struct BkHandle(uint Handle, bool Alive)
{
    public readonly uint Handle = Handle;
    public readonly bool Alive = Alive;

    public static implicit operator uint(BkHandle typed) => typed.Handle;

    public bool IsValid => Handle > 0 && Alive;
}