using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Renderer.Registry;

internal sealed class RenderRegistry
{
    public readonly RenderShaderRegistry ShaderRegistry;
    public readonly RenderUboRegistry UboRegistry;
    public readonly RenderFboRegistry FboRegistry;


    public RenderRegistry(GfxContext gfx)
    {
        ShaderRegistry = new RenderShaderRegistry(gfx);
        UboRegistry = new RenderUboRegistry(gfx);
        FboRegistry = new RenderFboRegistry(gfx);
        SetupGateway(gfx.ResourceManager.GetGfxApi());
    }

    private unsafe void SetupGateway(GfxResourceApi gfxApi)
    {
        RenderRegistryGateway.Setup(FboRegistry, UboRegistry);
        gfxApi.BindMetaChanged<UniformBufferId, UniformBufferMeta>(&RenderRegistryGateway.OnUboChanged);
        gfxApi.BindMetaChanged<FrameBufferId, FrameBufferMeta>(&RenderRegistryGateway.OnFboChange);
    }

    public void BeginRegistration(Size2D outputSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Width, 1, nameof(outputSize));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Height, 1, nameof(outputSize));

        UboRegistry.BeginRegistration();
        FboRegistry.BeginRegistration();
        //FboRegistry.RegisterTemp();
    }

    public void FinishRegistration()
    {
        UboRegistry.FinishRegistration();
        FboRegistry.FinishRegistration();
        ShaderRegistry.FinishRegistration();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderShader GetRenderShader(ShaderId shaderId) => ShaderRegistry.GetRenderShader(shaderId);

    public RenderUbo GetRenderUbo<TUbo>() where TUbo : class => UboRegistry.GetRenderUbo<TUbo>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderFbo GetRenderFbo(FboTagKey key) => FboRegistry.GetRenderFbo(key)!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetRenderFbo(FboTagKey key, out RenderFbo? fbo) => FboRegistry.TryGetRenderFbo(key, out fbo);
}