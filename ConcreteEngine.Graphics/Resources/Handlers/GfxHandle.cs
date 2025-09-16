namespace ConcreteEngine.Graphics.Resources;

public readonly record struct GfxHandle(uint Slot, ushort Gen, ResourceKind Kind)
{
    public bool IsValid =>  Gen > 0 && Kind != ResourceKind.Invalid;
}
