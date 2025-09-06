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
    private readonly DrawProcessor _drawRegistry;

    private IRender _render;
    private SceneDrawProducer _sceneDrawProducer = null!;
    private CommandProducerContext cmdProducerCtx = null!;

    public ICamera Camera => _render.Camera;

    internal RenderSystem(IGraphicsDevice graphics, MaterialStore materialStore)
    {
        _graphics = graphics;
        _gfx = graphics.Gfx;
        _materialStore = materialStore;

        _drawRegistry = new DrawProcessor(_graphics, _materialStore);

        _commandCollector = new DrawCommandCollector();
        _commandSubmitter = new RenderPipeline();
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

        _drawRegistry.Register<MeshDrawer, DrawCommandMesh>();
        _drawRegistry.Register<TerrainDrawer, DrawCommandTerrain>();
        _drawRegistry.Register<TilemapDrawer, DrawCommandTilemap>();
        _drawRegistry.Register<SpriteDrawer, DrawCommandSprite>();
        _drawRegistry.Register<LightDrawer, DrawCommandLight>();
        _drawRegistry.Register<SkyboxDrawer, DrawCommandSkybox>();

        _commandSubmitter.Initialize();

        _commandSubmitter.Register<DrawCommandMesh>(DrawCommandId.Mesh);
        _commandSubmitter.Register<DrawCommandTerrain>(DrawCommandId.Terrain);
        _commandSubmitter.Register<DrawCommandSkybox>( DrawCommandId.Skybox);
        _commandSubmitter.Register<DrawCommandSprite> ( DrawCommandId.Sprite);
        _commandSubmitter.Register<DrawCommandTilemap>( DrawCommandId.Tilemap);
        _commandSubmitter.Register<DrawCommandLight>( DrawCommandId.Light);
        
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

    internal void Render(float alpha, in FrameMetaInfo frameCtx, in RenderGlobalSnapshot renderGlobals,
        out FrameRenderResult result)
    {
        if (frameCtx.ViewportSize != _render.Camera.ViewportSize)
            _render.Camera.ViewportSize = frameCtx.ViewportSize;
        
        _sceneDrawProducer.SetSceneGlobals(in renderGlobals);

        _graphics.StartFrame(in frameCtx);
        PrepareRenderer(alpha, in renderGlobals);
        Execute(alpha, in renderGlobals);
        _graphics.EndFrame(out result);

        _commandSubmitter.Reset();
    }

    private void PrepareRenderer(float alpha, in RenderGlobalSnapshot renderGlobals)
    {
        _render.PrepareRender(alpha, in renderGlobals);
        _drawRegistry.Prepare(in renderGlobals);
        _commandCollector.Collect(alpha, _commandSubmitter);
        _commandSubmitter.Prepare();
    }

    private void Execute(float alpha, in RenderGlobalSnapshot renderGlobals)
    {
        foreach (var (renderTarget, passes) in _render)
        {
            foreach (var pass in passes)
            {
                _gfx.SetBlendMode(pass.Blend);
                _gfx.SetDepthTest(pass.DepthTest);
                ExecutePass(renderTarget, pass);
            }
        }
    }

    private void ExecutePass(RenderTargetId target, IRenderPassDescriptor pass)
    {
        if (pass is BlitRenderPass blitPass)
        {
            _gfx.BlitFramebuffer(blitPass.BlitFbo, blitPass.TargetFbo, blitPass.LinearFilter);
            return;
        }

        var isScreenPass = pass.TargetFbo == default;

        if (pass.TargetFbo == default)
            _gfx.BeginScreenPass(pass.Clear?.ClearColor, pass.Clear?.ClearMask);
        else
            _gfx.BeginRenderPass(pass.TargetFbo, pass.Clear?.ClearColor, pass.Clear?.ClearMask);

        
        switch (pass)
        {
            case IScenePass scenePass:
                _render.RenderScenePass(scenePass, _commandSubmitter); // handles Scene/Light via runtime type
                break;
            case IFsqPass fsq:
                DrawFullscreenQuad(fsq);
                break;
            case IDepthPass depthPass:
                _render.RenderDepthPass(depthPass, _commandSubmitter);
                break;
        }
        
        if (pass.Op == RenderPassOp.DrawScene)
        {

            _gfx.EndRenderPass();
            return;
        }

        if (pass.Op == RenderPassOp.FullscreenQuad && pass is IFsqPass fsqPass)
        {
            DrawFullscreenQuad(fsqPass);
        }

        if (!isScreenPass)
        {
            _gfx.EndRenderPass();
        }
    }

    private void DrawFullscreenQuad(IFsqPass pass)
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