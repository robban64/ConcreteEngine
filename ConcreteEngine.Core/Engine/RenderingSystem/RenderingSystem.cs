#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Engine.Platform;
using ConcreteEngine.Core.Engine.RenderingSystem.Batching;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Core.Rendering.Descriptors;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;
using RenderFrameInfo = ConcreteEngine.Core.Rendering.State.RenderFrameInfo;

#endregion

namespace ConcreteEngine.Core.Engine.RenderingSystem;

public interface IRenderingSystem : IGameEngineSystem
{
    RenderSceneProps SceneProperties { get; }
    BatcherRegistry Batchers { get; }
}

public sealed class RenderingSystem : IRenderingSystem
{
    public BatcherRegistry Batchers { get; }
    public RenderSceneProps SceneProperties { get; }
    public RenderSceneSnapshot SceneSnapshot => SceneProperties.Snapshot;

    private readonly IEngineWindowHost _window;
    private readonly GraphicsRuntime _graphics;
    private readonly RenderEngine _renderer;
    private readonly AssetSystem _assets;

    private readonly RenderEntityBus _renderEntityBus;

    internal RenderingSystem(IEngineWindowHost window, GraphicsRuntime graphics, AssetSystem assets)
    {
        _window = window;
        _graphics = graphics;
        _assets = assets;
        _graphics = graphics;
        SceneProperties = new RenderSceneProps();
        Batchers = new BatcherRegistry();

        _renderer = new RenderEngine(graphics, SceneProperties.Snapshot);
        _renderEntityBus = new RenderEntityBus();
    }


    internal void AttachWorld(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        _renderEntityBus.AttachWorld(world);
    }

    internal void RenderEmptyFrame(in RenderFrameInfo frameInfo) => _renderer.RenderEmptyFrame(in frameInfo);

    internal void PreRender(
        in RenderFrameInfo frameInfo,
        in RenderRuntimeParams runtimeParams,
        in RenderViewSnapshot viewSnapshot)
    {
        _renderEntityBus.Reset();
        _renderEntityBus.CollectEntities();

        var snapshot = SceneProperties.Commit();
        //_sceneDrawProducer.SetSceneGlobals(snapshot);
        _renderer.PrepareFrame(in frameInfo, in runtimeParams, in viewSnapshot);
    }

    internal void FillDrawBuffers(float alpha)
    {
        _renderEntityBus.FlushEntities(_renderer.CommandBuffer);
        SubmitMaterialData();
        _renderer.FillDrawBuffers();
    }

    internal void ExecuteFrame(BeginFrameStatus status, out GfxFrameResult frameResult)
    {
        _renderer.StartFrame(status);
        _renderer.UploadFrameData();
        _renderer.Render();
        _renderer.EndRenderFrame(out frameResult);
    }

    private void SubmitMaterialData()
    {
        var matStore = _assets.Materials;

        Span<TextureSlotInfo> slots = stackalloc TextureSlotInfo[RenderLimits.TextureSlots];
        foreach (var material in matStore.MaterialSpan)
        {
            var length = matStore.FillTextureInfo(material!, slots);
            matStore.GetMaterialUploadData(material!, out var payload);
            _renderer.SubmitMaterialDrawData(in payload, slots.Slice(0, length));
        }
    }

    public void Shutdown()
    {
    }


    internal RenderSetupBuilder Initialize(Action<GfxContext, BatcherRegistry> batcherSetup)
    {
        batcherSetup(_graphics.Gfx, Batchers);
        return _renderer.StartBuilder(_window.OutputSize);
    }

    internal void SetupRenderer(RenderSetupBuilder builder)
    {
        var shaderCount = _assets.Store.GetMetaSnapshot<Shader>().Count;

        builder.RegisterShader(shaderCount, ExtractShaderIds).RegisterCoreShaders(GetCoreShaders);

        builder.RegisterShader(shaderCount,
                (span) => _assets.Store.ExtractSpan<Shader, ShaderId>(span, static shader => shader.ResourceId))
            .RegisterCoreShaders(() => new RenderCoreShaders
            {
                DepthShader = _assets.Store.GetByName<Shader>("Depth").ResourceId,
                ColorFilterShader = _assets.Store.GetByName<Shader>("ColorFilter").ResourceId,
                CompositeShader = _assets.Store.GetByName<Shader>("Composite").ResourceId,
                PresentShader = _assets.Store.GetByName<Shader>("Present").ResourceId
            });


        builder.RegisterFbo<ShadowPassTag>(FboVariant.Default,
            new RegisterFboEntry().AttachDepthTexture(GfxFboDepthTextureDesc.Default())
                .UseFixedSize(new Size2D(2048, 2048)));

        builder.RegisterFbo<ScenePassTag>(FboVariant.Default,
            new RegisterFboEntry().AttachColorTexture(GfxFboColorTextureDesc.Off(), RenderBufferMsaa.X4)
                .AttachDepthStencilBuffer());

        builder.RegisterFbo<ScenePassTag>(FboVariant.Secondary,
            new RegisterFboEntry()
                .AttachColorTexture(GfxFboColorTextureDesc.DefaultMip())
                .AttachDepthStencilBuffer());

        builder.RegisterFbo<PostPassTag>(FboVariant.Default,
            new RegisterFboEntry().AttachColorTexture(GfxFboColorTextureDesc.Default()));

        builder.RegisterFbo<PostPassTag>(FboVariant.Secondary,
            new RegisterFboEntry().AttachColorTexture(GfxFboColorTextureDesc.Default()));


        builder.SetupPassPipeline(RenderPipelineVersion.Default3D);
        _renderer.ApplyBuilder(builder);
        return;

        void ExtractShaderIds(Span<ShaderId> span)
            => _assets.Store.ExtractSpan<Shader, ShaderId>(span, static shader => shader.ResourceId);

        RenderCoreShaders GetCoreShaders() => new()
        {
            DepthShader = _assets.Store.GetByName<Shader>("Depth").ResourceId,
            ColorFilterShader = _assets.Store.GetByName<Shader>("ColorFilter").ResourceId,
            CompositeShader = _assets.Store.GetByName<Shader>("Composite").ResourceId,
            PresentShader = _assets.Store.GetByName<Shader>("Present").ResourceId
        };
    }
}