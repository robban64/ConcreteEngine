using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render;

[StructLayout(LayoutKind.Sequential)]
public readonly struct MaterialMeta(
    ShaderId shaderId,
    RangeU16 bindingRange,
    byte bindingCapacity,
    GfxDrawState drawState,
    GfxDrawFunctions drawFunctions)
{
    public readonly GfxDrawState DrawState = drawState;
    public readonly GfxDrawFunctions DrawFunctions = drawFunctions;
    public readonly RangeU16 BindingRange = bindingRange;
    public readonly ShaderId ShaderId = shaderId;
    public readonly byte BindingCapacity = bindingCapacity;
}