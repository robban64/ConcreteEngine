using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Renderer.Buffer;

public struct EffectUniformParams(ColorRgba color)
{
    public ColorRgba Color = color;
}