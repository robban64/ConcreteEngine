using ConcreteEngine.Common;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Editor.Data;
using ConcreteEngine.Engine.Editor.Definitions;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds.View;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.State;

namespace ConcreteEngine.Engine.Worlds.Render;

public sealed class WorldRenderer
{
    private readonly EngineWindow _window;
    private readonly GfxContext _graphics;
    private readonly RenderEngine _renderer;
    private readonly AssetSystem _assets;
    private readonly Camera3D _camera;

    private readonly WorldRenderParams _worldRenderParams;

    private readonly DrawEntityAssembler _drawEntities;

    private bool _hasUploadedMaterial = false;


    internal WorldRenderer(EngineWindow window, GraphicsRuntime graphics, AssetSystem assets,
        WorldRenderParams worldRenderParams, DrawEntityAssembler drawEntities, Camera3D camera)
    {
        _window = window;
        _graphics = graphics.Gfx;
        _assets = assets;
        _worldRenderParams = worldRenderParams;
        _drawEntities = drawEntities;
        _camera = camera;

        PrimitiveMeshes.CreatePrimitives(graphics.Gfx.Meshes);
        InvalidOpThrower.ThrowIf(PrimitiveMeshes.FsqQuad == 0 || PrimitiveMeshes.SkyboxCube == 0);

        _renderer = new RenderEngine(graphics, _worldRenderParams.Snapshot, PrimitiveMeshes.FsqQuad);
    }

    internal RenderEngine RenderEngine => _renderer;
    internal RenderCamera RenderCamera => _renderer.RenderCamera;


    internal void RecreateFrameBuffer(FboCommandRecord req)
    {
        _graphics.Commands.BindFramebuffer(default);
        _graphics.Commands.UnbindAllTextures();

        switch (req.Action)
        {
            case FboCommandAction.RecreateScreenDependentFbo:
                _renderer.FboRegistry.RecreateScreenDependentFbo(_window.OutputSize);
                break;
            case FboCommandAction.RecreateShadowFbo:
                if (_worldRenderParams.SetShadow(req.Size.Width))
                    _renderer.FboRegistry.RecreateFixedFrameBuffer<ShadowPassTag>(FboVariant.Default, req.Size);
                break;
            case FboCommandAction.None:
            default:
                throw new ArgumentOutOfRangeException(nameof(req.Action));
        }
    }

    internal void RenderEmptyFrame(in RenderFrameInfo frameInfo)
    {
        _renderer.RenderEmptyFrame(frameInfo);
    }


    internal void PreRender(
        BeginFrameStatus status,
        RenderFrameInfo frameInfo,
        RenderRuntimeParams runtimeParams)
    {
        _drawEntities.Reset();

        _camera.WriteSnapshot(EngineTime.GameAlpha, RenderCamera);

        _renderer.PrepareFrame(in frameInfo, in runtimeParams);

        // Upload materials
        SubmitMaterialData();

        // Upload draw commands
        _drawEntities.Execute(RenderEngine.CommandBuffer);

        // fill buffers
        _renderer.CollectDrawBuffers();

        _renderer.StartFrame(status);
    }

    internal void ExecuteFrame(out GfxFrameResult frameResult)
    {
        _renderer.UploadFrameData();

        _renderer.Render();

        _renderer.EndRenderFrame(out frameResult);
    }


    private void SubmitMaterialData()
    {
        var matStore = _assets.MaterialStoreImpl;
        var isDirty = false;
        foreach (var material in matStore.MaterialSpan)
        {
            if (material?.State.IsDirty != true) continue;
            material.State.ClearDirty();
            isDirty = true;
            _hasUploadedMaterial = false;
        }

        if (!isDirty && _hasUploadedMaterial) return;
        foreach (var material in matStore.MaterialSpan)
        {
            matStore.GetMaterialUploadData(material!, out var payload);
            _renderer.SubmitMaterialDrawData(in payload, material!.TextureSlots.CacheSlots);
        }

        _hasUploadedMaterial = true;
    }


    public void Shutdown()
    {
    }


    internal RenderSetupBuilder StartBuilder() => _renderer.StartBuilder(_window.OutputSize);

    internal void SetupRenderer(RenderSetupBuilder builder)
    {
        var shaderCount = _assets.Store.GetMetaSnapshot<Shader>().Count;

        builder.RegisterShader(shaderCount, ExtractShaderIds).RegisterCoreShaders(GetCoreShaders);
        WorldRenderSetup.RegisterFrameBuffers(builder, _worldRenderParams);
        builder.SetupPassPipeline(RenderPipelineVersion.Default3D);
        _renderer.ApplyBuilder(builder);
        return;

        void ExtractShaderIds(Span<ShaderId> span) =>
            _assets.Store.ExtractSpan<Shader, ShaderId>(span, static shader => shader.ResourceId);

        RenderCoreShaders GetCoreShaders() => WorldRenderSetup.GetCoreShaders(_assets.Store);
    }
}