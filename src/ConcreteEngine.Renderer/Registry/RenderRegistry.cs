using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Renderer.Registry;

public sealed class RenderRegistry
{
    public readonly RenderShaderRegistry ShaderRegistry;
    public readonly RenderUboRegistry UboRegistry;
    public readonly RenderFboRegistry FboRegistry;


    internal RenderRegistry(GfxContext gfx)
    {
        ShaderRegistry = new RenderShaderRegistry(gfx);
        UboRegistry = new RenderUboRegistry(gfx);
        FboRegistry = new RenderFboRegistry(gfx);
        SetupGateway(gfx.ResourceManager.GetGfxApi());
    }

    internal void SetupGateway(GfxResourceApi gfxApi)
    {
        gfxApi.BindMetaChanged(GraphicsKind.FrameBuffer, FboRegistry.OnFboChange);
        gfxApi.BindMetaChanged(GraphicsKind.UniformBuffer, UboRegistry.OnUboChanged);
    }

    internal void BeginRegistration(Size2D outputSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Width, 1, nameof(outputSize));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Height, 1, nameof(outputSize));

        UboRegistry.BeginRegistration();
        FboRegistry.BeginRegistration(outputSize);
        //FboRegistry.RegisterTemp();
    }

    internal void FinishRegistration()
    {
        UboRegistry.FinishRegistration();
        FboRegistry.FinishRegistration();
        ShaderRegistry.FinishRegistration();
    }
}