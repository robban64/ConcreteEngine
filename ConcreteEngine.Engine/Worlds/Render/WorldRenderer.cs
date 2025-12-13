#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Editor.Data;
using ConcreteEngine.Engine.Editor.Definitions;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Engine.Worlds.View;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.State;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render;

public interface IWorldRenderer : IGameEngineSystem
{
    WorldRenderParams WorldRenderParams { get; }
}

public sealed class WorldRenderer : IWorldRenderer
{
    public WorldRenderParams WorldRenderParams { get; }

    private readonly EngineWindow _window;
    private readonly GraphicsRuntime _graphics;
    private readonly RenderEngine _renderer;
    private readonly AssetSystem _assets;

    private readonly MeshTable _meshTable;
    private readonly MaterialTable _materialTable;
    private readonly AnimationTable _animationTable;

    private readonly RenderEntityBus _renderEntityBus;

    private readonly EngineEventBus _eventBus;

    private bool _hasUploadedMaterial = false;

    internal RenderEngine RenderEngine => _renderer;
    internal RenderCamera RenderCamera => _renderer.RenderCamera;

    internal WorldRenderer(EngineWindow window, GraphicsRuntime graphics, AssetSystem assets,
        EngineEventBus eventBus, WorldRenderParams worldRenderParams)
    {
        _window = window;
        _graphics = graphics;
        _assets = assets;
        _graphics = graphics;
        _eventBus = eventBus;
        WorldRenderParams = worldRenderParams;

        PrimitiveMeshes.CreatePrimitives(graphics.Gfx.Meshes);
        InvalidOpThrower.ThrowIf(PrimitiveMeshes.FsqQuad == 0 || PrimitiveMeshes.SkyboxCube == 0);

        _renderer = new RenderEngine(graphics, WorldRenderParams.Snapshot, PrimitiveMeshes.FsqQuad);

        _meshTable = new MeshTable();
        _materialTable = new MaterialTable();
        _animationTable = new AnimationTable();

        _renderEntityBus = new RenderEntityBus();
    }


    internal void AttachWorld(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        _meshTable.Setup(_assets);
        _animationTable.Setup(_assets);
        _renderEntityBus.AttachWorld(world);
        world.AttachRender(_graphics.Gfx, _meshTable, _materialTable, _animationTable);

        _renderEntityBus.CubeId = _assets.StoreImpl.GetByName<Model>("Cube").ModelId;

        var mat = _assets.MaterialStoreImpl.CreateMaterial("EmptyMat", "EmptyMat1");
        _renderEntityBus.EmptyMaterialKey = _materialTable.Add(MaterialTagBuilder.BuildOne(mat.Id, true));

        // PrepareRenderView(1, world.Camera);

        DrawDataProvider.Attach(_renderer.CommandBuffer, _animationTable, _meshTable, _materialTable, world.Entities);
    }

    internal void RenderEmptyFrame(in RenderFrameInfo frameInfo) => _renderer.RenderEmptyFrame(in frameInfo);

    internal void RecreateFrameBuffer(FboCommandRecord req)
    {
        _graphics.Gfx.Commands.BindFramebuffer(default);
        _graphics.Gfx.Commands.UnbindAllTextures();

        switch (req.Action)
        {
            case FboCommandAction.RecreateScreenDependentFbo:
                _renderer.FboRegistry.RecreateScreenDependentFbo(_window.OutputSize);
                break;
            case FboCommandAction.RecreateShadowFbo:
                _renderer.FboRegistry.RecreateFixedFrameBuffer<ShadowPassTag>(FboVariant.Default, req.Size);
                WorldRenderParams.SetShadow(req.Size.Width);
                break;
            case FboCommandAction.None:
            default:
                throw new ArgumentOutOfRangeException(nameof(req.Action));
        }
    }


    internal void PreRender(
        BeginFrameStatus status,
        RenderFrameInfo frameInfo,
        RenderRuntimeParams runtimeParams,
        Camera3D camera)
    {
        _renderEntityBus.Reset();

        WorldRenderParams.Commit();

        PrepareRenderView(in frameInfo, camera);

        _renderer.PrepareFrame(in frameInfo, in runtimeParams);

        // Upload materials
        SubmitMaterialData();

        // Upload draw commands
        _renderEntityBus.Execute();

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


    private void PrepareRenderView(in RenderFrameInfo frameInfo, Camera3D camera)
    {
        camera.WriteSnapshot(EngineTime.GameAlpha, ref RenderCamera.RenderView);
        DrawDataProvider.FrameInfo = frameInfo;
        DrawDataProvider.RenderView = RenderCamera.RenderView;
        DrawDataProvider.Frustum = camera.Frustum;
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


    internal RenderSetupBuilder Initialize()
    {
        return _renderer.StartBuilder(_window.OutputSize);
    }

    internal void SetupRenderer(RenderSetupBuilder builder)
    {
        var shaderCount = _assets.Store.GetMetaSnapshot<Shader>().Count;

        builder.RegisterShader(shaderCount, ExtractShaderIds).RegisterCoreShaders(GetCoreShaders);
        WorldRenderSetup.RegisterFrameBuffers(builder, WorldRenderParams);
        builder.SetupPassPipeline(RenderPipelineVersion.Default3D);
        _renderer.ApplyBuilder(builder);
        return;

        void ExtractShaderIds(Span<ShaderId> span) =>
            _assets.Store.ExtractSpan<Shader, ShaderId>(span, static shader => shader.ResourceId);

        RenderCoreShaders GetCoreShaders() => WorldRenderSetup.GetCoreShaders(_assets.Store);
    }
}