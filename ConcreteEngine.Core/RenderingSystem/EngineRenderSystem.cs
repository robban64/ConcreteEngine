#region

using System.Diagnostics;
using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Diagnostics.Utility;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Meshes;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.RenderingSystem.Batching;
using ConcreteEngine.Core.RenderingSystem.Primitives;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Descriptors;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.State;

#endregion

namespace ConcreteEngine.Core.RenderingSystem;

public interface IRenderingSystem : IGameEngineSystem
{
    RenderSceneProps SceneProperties { get; }
    BatcherRegistry Batchers { get; }
}

public sealed class EngineRenderSystem : IRenderingSystem
{
    public BatcherRegistry Batchers { get; }
    public RenderSceneProps SceneProperties { get; }
    public RenderSceneSnapshot SceneSnapshot => SceneProperties.Snapshot;
    
    private readonly EngineWindow _window;
    private readonly GraphicsRuntime _graphics;
    private readonly RenderEngine _renderer;
    private readonly AssetSystem _assets;

    private readonly MeshTable _meshTable;
    private readonly RenderEntityBus _renderEntityBus;

    internal RenderEngine RenderEngine => _renderer;

    private readonly EngineEventBus _eventBus;
    
    
    private bool _hasUploadedMaterial = false;

    internal EngineRenderSystem(EngineWindow window, GraphicsRuntime graphics, AssetSystem assets,
        EngineEventBus eventBus)
    {
        _window = window;
        _graphics = graphics;
        _assets = assets;
        _graphics = graphics;
        _eventBus = eventBus;
        SceneProperties = new RenderSceneProps();
        Batchers = new BatcherRegistry();


        PrimitiveMeshes.CreatePrimitives(graphics.Gfx.Meshes);
        InvalidOpThrower.ThrowIf(PrimitiveMeshes.FsqQuad == 0 || PrimitiveMeshes.SkyboxCube == 0);

        _renderer = new RenderEngine(graphics, SceneProperties.Snapshot, PrimitiveMeshes.FsqQuad);

        _meshTable = new MeshTable();
        _renderEntityBus = new RenderEntityBus(_meshTable);
    }


    internal void AttachWorld(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        _meshTable.Setup(_assets);
        _renderEntityBus.AttachWorld(world);
        world.AttachModelRegistry(_meshTable);
    }

    internal void RenderEmptyFrame(in RenderFrameInfo frameInfo) => _renderer.RenderEmptyFrame(in frameInfo);

    internal void OnRecreateFrameBuffer(in RecreateRequest req)
    {
        _graphics.Gfx.Commands.BindFramebuffer(default);
        _graphics.Gfx.Commands.UnbindAllTextures();

        if (req.SpecialAction == RecreateSpecialAction.RecreateScreenDependentFbo)
            _renderer.FboRegistry.RecreateScreenDependentFbo(_window.OutputSize);
        else if (req.SpecialAction == RecreateSpecialAction.RecreateShadowFbo)
        {
            SceneProperties.SetShadowDefault(req.Param0);
            _renderer.FboRegistry.RecreateFixedFrameBuffer<ShadowPassTag>(FboVariant.Default,
                new Size2D(req.Param0, req.Param0));
        }
    }

    private FrameTimer _frameTimer = new FrameTimer();

    internal void PreRender(
        BeginFrameStatus status,
        in RenderFrameInfo frameInfo,
        in RenderRuntimeParams runtimeParams,
        in RenderViewSnapshot viewSnapshot)
    {
        _renderEntityBus.Reset();

        SceneProperties.Commit();
        _renderer.PrepareFrame(in frameInfo, in runtimeParams, in viewSnapshot);
        SubmitMaterialData();
        // fill buffers
        _renderEntityBus.CollectEntities();
        _frameTimer.Begin();
        _renderEntityBus.FlushEntities(_renderer.CommandBuffer);
        if(_frameTimer.End(out _)) Console.WriteLine(_frameTimer.ResultString);
        _renderer.CollectDrawBuffers();
        _renderer.StartFrame(status);
    }

    internal void ExecuteFrame(out GfxFrameResult frameResult)
    {
        _renderer.UploadFrameData();
        _renderer.Render();
        _renderer.EndRenderFrame(out frameResult);

        ClearMaterialDirty();
    }
    

    private void SubmitMaterialData()
    {
        var matStore = _assets.MaterialStoreImpl;
        var isDirty = false;
        foreach (var material in matStore.MaterialSpan)
        {
            if (material?.State.Dirty != true) continue;
            isDirty = true;
            _hasUploadedMaterial = false;
        }
        
        if(!isDirty && _hasUploadedMaterial) return;
        

        foreach (var material in matStore.MaterialSpan)
        {
            matStore.GetMaterialUploadData(material!, out var payload);
            _renderer.SubmitMaterialDrawData(in payload, material!.TextureSlots.CacheSlots);
        }

        _hasUploadedMaterial = true;
    }

    private void ClearMaterialDirty()
    {
        var matStore = _assets.MaterialStoreImpl;
        foreach (var material in matStore.MaterialSpan)
        {
            if(material == null || !material.State.Dirty) continue;
            material.State.ClearDirty();
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