using System.Runtime.InteropServices;

namespace ConcreteEngine.Graphics.Handles;

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct GfxHandle(int Slot, ushort Gen, GraphicsKind Kind)
{
    public readonly int Slot = Slot;
    public readonly ushort Gen = Gen;
    public readonly GraphicsKind Kind = Kind;
    public bool IsValid => Gen > 0 && Kind != GraphicsKind.Invalid;
}