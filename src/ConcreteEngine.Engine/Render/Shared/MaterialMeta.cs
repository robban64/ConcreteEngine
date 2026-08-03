using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render;

[StructLayout(LayoutKind.Sequential)]
public struct MaterialMeta(ShaderId shaderId, RangeU16 bindingRange, GfxDrawState drawState, GfxDrawFunctions drawFunctions)
{
    public GfxDrawState DrawState = drawState;
    public GfxDrawFunctions DrawFunctions = drawFunctions;
    public RangeU16 BindingRange = bindingRange;
    public ShaderId ShaderId = shaderId;
}