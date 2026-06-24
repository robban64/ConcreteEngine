using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Renderer.Registry;

public sealed class RenderRegistry
{
    public readonly RenderShaderRegistry ShaderRegistry;
    public readonly RenderUboRegistry UboRegistry;
    public readonly RenderFboRegistry FboRegistry;

    internal RenderRegistry(GfxContext gfx)
    {
        ShaderRegistry = new RenderShaderRegistry();
        UboRegistry = new RenderUboRegistry(gfx);
        FboRegistry = new RenderFboRegistry(gfx);
    }

    internal void BeginRegistration(Size2D outputSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Width, 1, nameof(outputSize));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Height, 1, nameof(outputSize));

        UboRegistry.BeginRegistration();
        FboRegistry.BeginRegistration(outputSize);
    }

    internal void FinishRegistration()
    {
        UboRegistry.FinishRegistration();
        FboRegistry.FinishRegistration();
        ShaderRegistry.FinishRegistration();

        GfxResourceApi.BindMetaChanged(GraphicsKind.FrameBuffer, FboRegistry.OnFboChange);
        GfxResourceApi.BindMetaChanged(GraphicsKind.UniformBuffer, UboRegistry.OnUboChanged);
    }
}