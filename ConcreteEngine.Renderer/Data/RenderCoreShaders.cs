using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Renderer.Data;

public readonly struct RenderCoreShaders
{
    public required ShaderId DepthShader { get; init; }
    public required ShaderId CompositeShader { get; init; }
    public required ShaderId ColorFilterShader { get; init; }
    public required ShaderId PresentShader { get; init; }
    public required ShaderId HighlightShader { get; init; }
    public required ShaderId BoundingBoxShader { get; init; }
    public required ShaderId ParticleShader { get; init; }
}