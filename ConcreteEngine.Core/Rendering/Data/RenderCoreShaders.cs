using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Core.Rendering.Data;

public readonly record struct RenderCoreShaders
{
    public ShaderId DepthShader { get; init;}
    public ShaderId CompositeShader { get; init; }
    public ShaderId ColorFilterShader { get; init; }
    public ShaderId PresentShader { get; init; }
}