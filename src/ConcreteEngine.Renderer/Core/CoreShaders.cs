using ConcreteEngine.Graphics.Handles;

namespace ConcreteEngine.Renderer.Core;

public readonly struct CoreShaders
{
    public required ShaderId DepthShader { get; init; }
    public required ShaderId CompositeShader { get; init; }
    public required ShaderId ColorFilterShader { get; init; }
    public required ShaderId PresentShader { get; init; }
    public required ShaderId HighlightShader { get; init; }
    public required ShaderId BoundingBoxShader { get; init; }
}