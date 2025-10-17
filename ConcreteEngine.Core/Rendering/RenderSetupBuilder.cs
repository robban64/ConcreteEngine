using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Batching;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Draw;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Core.Rendering;

public abstract class RenderBuilderStage
{
    protected readonly RenderBuilderContext Ctx;
    
    protected RenderBuilderStage(RenderBuilderContext ctx) => Ctx = ctx;

    protected void ThrowIfBuilt()
    {
        if (Ctx.Done) throw new InvalidOperationException("Already built.");
    }

    protected TStage Next<TStage>(Func<RenderBuilderContext, TStage> f) where TStage : RenderBuilderStage
        => f(Ctx);
}

public sealed class RenderSetupBuilder
{
    private RenderBuilderContext Ctx { get; }
    
    private RenderRegistry _renderRegistry;

    private DrawCommandPipeline _drawPipeline;
    private RenderPassPipeline _passPipeline;

    private DrawStateOps _drawStateOps = null!;
    private DrawCommandProcessor _cmdDraw = null!;
    private DrawBuffers _drawBuffers = null!;

    public RenderSceneProps RenderProps { get; }
    private RenderSceneState _snapshot;

    
    private readonly BatcherRegistry _batches = new();
    private readonly RenderView _renderView = new();

    public RenderSetupBuilder(GfxContext gfx, Size2D outputSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Width, 4);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Height,4);

        Ctx = new RenderBuilderContext(gfx, outputSize);
    }
/*

    internal void SetupRegistry(ReadOnlySpan<ShaderId> shaderIds, in RenderCoreShaders coreShaders)
    {
        _renderRegistry = new RenderRegistry(_gfx);
        _renderRegistry.BeginRegistration(_initialSize);
        _renderRegistry.ShaderRegistry.RegisterCollection(shaderIds).RegisterCoreShader(in coreShaders);
        _renderRegistry.FinishRegistration();

    }
    
    internal void InitializeDraw()
    {
        var drawCtx = new DrawStateContext(_renderRegistry);
        var drawCtxPayload = new DrawStateContextPayload
        {
            Gfx = _gfx, Registry = _renderRegistry, RenderView = _renderView, Snapshot = _snapshot
        };

        _drawBuffers = new DrawBuffers(drawCtx, drawCtxPayload);
        _cmdDraw = new DrawCommandProcessor(drawCtx, drawCtxPayload, _drawBuffers);
        _drawStateOps = new DrawStateOps(drawCtx, drawCtxPayload, _drawBuffers);

        _batches.Register(new TerrainBatcher(_gfx));
        //_batches.Register(new SpriteBatcher(_gfx));
        //_batches.Register(new TilemapBatcher(_gfx, 64, 32));


        _drawPipeline = new DrawCommandPipeline();
        _drawPipeline.Initialize(_gfx, _batches, _cmdDraw, _drawBuffers);

        _cmdDraw.Initialize();
        _drawBuffers.AttachMaterialBuffer(_drawPipeline.MaterialBuffer);

        RegisterPasses();
        _initialized = true;
    }

    private void RegisterPasses()
    {
        _passPipeline = new RenderPassPipeline(_drawStateOps, _renderRegistry);
        TempPassSetup.RegisterPassPipeline(_passPipeline, in _renderRegistry.ShaderRegistry.CoreShaders);
    }
    */
}