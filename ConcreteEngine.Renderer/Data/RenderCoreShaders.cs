#region

using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Renderer.Data;

public readonly struct RenderCoreShaders
{
    public required ShaderId DepthShader { get; init; }
    public required ShaderId CompositeShader { get; init; }
    public required ShaderId ColorFilterShader { get; init; }
    public required ShaderId PresentShader { get; init; }
    public required ShaderId HighlightShader { get; init; }
}
