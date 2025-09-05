#region

using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Extensions;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Systems;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;
using static ConcreteEngine.Core.Rendering.RenderConsts;

#endregion

namespace ConcreteEngine.Core.Rendering;

public enum RenderType
{
    Render2D,
    Render3D
}

public interface IRenderSystem : IGameEngineSystem
{
    ICamera Camera { get; }

    TSink GetSink<TSink>() where TSink : IDrawSink;
    Material CreateMaterial(string templateName);
    void MutateRenderPass(RenderTargetId targetId, in RenderPassMutation mutation);
}

public sealed class RenderSystem : IRenderSystem
{
    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _gfx;
    private readonly MaterialStore _materialStore;
    private readonly DrawCommandCollector _commandCollector;
    private readonly RenderPipeline _commandSubmitter;
    private readonly BatcherRegistry _batches = new();
    private readonly CommandDrawerRegistry _drawRegistry;

    private IRender _render;
    private SceneDrawProducer _sceneDrawProducer = null!;
    private CommandProducerContext cmdProducerCtx = null!;

    public ICamera Camera => _render.Camera;

    internal RenderSystem(IGraphicsDevice graphics, MaterialStore materialStore)
    {
        _graphics = graphics;
        _gfx = graphics.Gfx;
        _materialStore = materialStore;

        _commandCollector = new DrawCommandCollector();
        _commandSubmitter = new RenderPipeline();
        _drawRegistry = new CommandDrawerRegistry(_graphics, _materialStore);
    }

    internal void Initialize(IGameFeatureManager features)
    {
        _batches.Register(new TerrainBatcher(_graphics));
        _batches.Register(new SpriteBatcher(_graphics));
        _batches.Register(new TilemapBatcher(_graphics, 64, 32));

        cmdProducerCtx = new CommandProducerContext
        {
            Graphics = _graphics,
            DrawBatchers = _batches,
        };

        // Collector
        _commandCollector.RegisterProducerSink<IMeshDrawSink>(new MeshDrawProducer());
        _commandCollector.RegisterProducerSink<ITerrainDrawSink>(new TerrainDrawProducer());

        _commandCollector.RegisterProducerSink<ITilemapDrawSink>(new TilemapDrawProducer());
        _commandCollector.RegisterProducerSink<ISpriteDrawSink>(new SpriteDrawProducer());
        _commandCollector.RegisterProducerSink<ILightDrawSink>(new LightProducer());
        
        _sceneDrawProducer = new  SceneDrawProducer();
        _commandCollector.RegisterProducer<SceneDrawProducer>(_sceneDrawProducer);

        _commandCollector.AttachContext(cmdProducerCtx);

        _drawRegistry.Register<MeshDrawer>();
        _drawRegistry.Register<TerrainDrawer>();
        _drawRegistry.Register<SpriteDrawer>();
        _drawRegistry.Register<LightDrawer>();
        _drawRegistry.Register<SkyboxDrawer>();

        _commandSubmitter.Initialize(_drawRegistry.Drawers);

        _commandSubmitter.Register<DrawCommandMesh, MeshDrawer>
            (DrawCommandTag.Mesh3D, DrawCommandId.Mesh);

        _commandSubmitter.Register<DrawCommandTerrain, TerrainDrawer>
            (DrawCommandTag.Terrain, DrawCommandId.Terrain);
        
        _commandSubmitter.Register<DrawCommandSkybox, SkyboxDrawer>
            (DrawCommandTag.Skybox, DrawCommandId.Skybox);


        _commandSubmitter.Register<DrawCommandSprite, SpriteDrawer>
            (DrawCommandTag.Mesh2D, DrawCommandId.Tilemap, DrawCommandId.Sprite);

        _commandSubmitter.Register<DrawCommandLight, LightDrawer>
            (DrawCommandTag.Effect2D, DrawCommandId.Light);


        _commandCollector.InitializeProducers();
    }

    internal void RegisterScene(RenderType renderType, RenderTargetDescriptor desc)
    {
        if (renderType == RenderType.Render2D)
            _render = new Render2D(_graphics, _materialStore);
        else
            _render = new Render3D(_graphics, _materialStore);
        
        _render.RegisterRenderTargetsFrom(desc);
        _drawRegistry.Initialize(null, (Render3D)_render);
    }

    public TSink GetSink<TSink>() where TSink : IDrawSink => _commandCollector.GetSink<TSink>();


    public Material CreateMaterial(string templateName)
        => _materialStore.CreateMaterialFromTemplate(templateName);

    public void MutateRenderPass(RenderTargetId targetId, in RenderPassMutation mutation)
        => _render.MutateRenderPass(targetId, in mutation);

    public void Shutdown()
    {
    }

    internal void BeginTick(in UpdateMetaInfo updateMeta) => _commandCollector.BeginTick(updateMeta);
    internal void EndTick() => _commandCollector.EndTick();

    internal void Render(float alpha, in FrameMetaInfo frameCtx, in SceneRenderGlobalSnapshot renderGlobals,
        out FrameRenderResult result)
    {
        if (frameCtx.ViewportSize != _render.Camera.ViewportSize)
            _render.Camera.ViewportSize = frameCtx.ViewportSize;
        
        _sceneDrawProducer.SetSceneGlobals(in renderGlobals);

        _graphics.StartFrame(in frameCtx);
        PrepareRenderer(alpha);
        Execute(alpha);
        _graphics.EndFrame(out result);

        _commandSubmitter.Reset();
    }

    private void PrepareRenderer(float alpha)
    {
        _render.PrepareRender(alpha);
        _commandCollector.Collect(alpha, _commandSubmitter);
        _commandSubmitter.Prepare();
    }

    private void Execute(float alpha)
    {
        foreach (var (renderTarget, passes) in _render)
        {
            foreach (var pass in passes)
            {
                //var (prevBlend, prevDepthTest) = (_gfx.BlendMode, _gfx.DepthTest);
                _gfx.SetBlendMode(pass.Blend);
                _gfx.SetDepthTest(pass.DepthTest);

                ExecutePass(renderTarget, pass);

                //_gfx.SetBlendMode(prevBlend);
                //_gfx.SetDepthTest(prevDepthTest);
            }
        }
    }

    private void ExecutePass(RenderTargetId target, IRenderPassDescriptor pass)
    {
        if (pass.Op == RenderPassOp.Blit && pass is BlitRenderPass blitPass)
        {
            // preserves bindings internally
            _gfx.BlitFramebuffer(blitPass.BlitFbo, blitPass.TargetFbo, blitPass.LinearFilter);
            return;
        }

        var isScreenPass = pass.TargetFbo == default;

        if (pass.TargetFbo == default)
            _gfx.BeginScreenPass(pass.Clear?.ClearColor, pass.Clear?.ClearMask);
        else
            _gfx.BeginRenderPass(pass.TargetFbo, pass.Clear?.ClearColor, pass.Clear?.ClearMask);

        if (pass.Op == RenderPassOp.DrawScene)
        {
            if (pass is SceneRenderPass scenePass)
                _render.RenderScenePass(scenePass, _commandSubmitter);

            if (pass is LightRenderPass lightPass)
                _render.RenderLightPass(lightPass, _commandSubmitter);

            _gfx.EndRenderPass();
            return;
        }


        if (pass.Op == RenderPassOp.FullscreenQuad && pass is FsqRenderPass fsqPass)
        {
            DrawFullscreenQuad(fsqPass);
        }

        if (!isScreenPass)
        {
            _gfx.EndRenderPass();
        }
    }

    private void DrawFullscreenQuad(FsqRenderPass pass)
    {
        ArgumentNullException.ThrowIfNull(pass);
        ArgumentNullException.ThrowIfNull(pass.SourceTextures);
        ArgumentOutOfRangeException.ThrowIfZero(pass.SourceTextures.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pass.SourceTextures.Length, 4, nameof(pass.SourceTextures));

        var viewport = _render.Camera.ViewportSize;
        _gfx.UseShader(pass.Shader);
        _gfx.SetUniform(ShaderUniform.TexelSize, viewport.ToSystemVec2() * pass.SizeRatio);

        for (int i = 0; i < pass.SourceTextures.Length; i++)
        {
            _gfx.BindTexture(pass.SourceTextures[i], (uint)i);
        }

        _gfx.BindMesh(_graphics.Primitives.FsqQuad);
        _gfx.DrawMesh();
    }

    private void DrawSkybox()
    {
    }
}