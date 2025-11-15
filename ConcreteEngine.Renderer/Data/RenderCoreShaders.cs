#region

using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Renderer.Data;

public readonly struct RenderCoreShaders
{
    public ShaderId DepthShader { get; init; }
    public ShaderId CompositeShader { get; init; }
    public ShaderId ColorFilterShader { get; init; }
    public ShaderId PresentShader { get; init; }
}