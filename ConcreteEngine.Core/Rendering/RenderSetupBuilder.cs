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
using ConcreteEngine.Graphics.Primitives;

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
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Height, 4);

        Ctx = new RenderBuilderContext(gfx, outputSize);
    }

    public void SetupRegistry(Action<RenderRegistryBuilder> builder)
    {
        _renderRegistry = new RenderRegistry(Ctx.Gfx);
        _renderRegistry.BeginRegistration(Ctx.OutputSize);
        builder(new RenderRegistryBuilder(_renderRegistry));
        _renderRegistry.FinishRegistration();
    }

    public void SetupBatchers(Action<GfxContext, BatcherRegistry> builder)
        => builder(Ctx.Gfx, _batches);

    public void SetupDrawPipeline(Action<IDrawCommandCollector> builder)
    {
        _drawPipeline = new DrawCommandPipeline(Ctx.Gfx, _batches, _renderRegistry, _renderView, _snapshot);
        builder(_drawPipeline.Collector);
        _drawPipeline.Initialize();
    }

    private void SetupPassPipeline()
    {
        _passPipeline = new RenderPassPipeline(_drawStateOps, _renderRegistry);
        PassPipeline3D.RegisterPassPipeline(_passPipeline, in _renderRegistry.ShaderRegistry.CoreShaders);
    }
}

public sealed class RenderRegistryBuilder
{
    private readonly RenderRegistry _renderRegistry;

    internal RenderRegistryBuilder(RenderRegistry renderRegistry) => _renderRegistry = renderRegistry;

    public RenderRegistryBuilder RegisterFbo(Action<IRenderFboRegistry> builder)
    {
        builder(_renderRegistry.FboRegistry);
        return this;
    }

    public RenderRegistryBuilder RegisterShader(int shaderCount, Action<Span<ShaderId>> builder)
    {
        Span<ShaderId> shaders = stackalloc ShaderId[shaderCount];
        builder(shaders);
        _renderRegistry.ShaderRegistry.RegisterCollection(shaders);
        return this;
    }

    public RenderRegistryBuilder RegisterCoreShaders(Func<RenderCoreShaders> builder)
    {
        _renderRegistry.ShaderRegistry.RegisterCoreShader(builder());
        return this;
    }
}