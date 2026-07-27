using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Engine.Render.Renderer;

public struct EffectUniformParams(ColorRgba color)
{
    public ColorRgba Color = color;
}

public struct EffectCommand
{
    public int SubmitIndex;
    public ColorRgba Color;
    public DrawCommandResolver Resolver;
}