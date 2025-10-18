using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Batching;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Core.Rendering.Descriptors;
using ConcreteEngine.Core.Rendering.Draw;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Core.Rendering;

public sealed class RenderSetupBuilder
{
    private RenderSystemContext SystemCtx { get; }
    private RenderBuilderContext Ctx { get; }

    public bool IsDone => Ctx.Done;

    internal RenderSetupBuilder(RenderSystemContext systemCtx, Size2D outputSize)
    {
        ArgumentNullException.ThrowIfNull(systemCtx);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Width, 4);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Height, 4);

        SystemCtx = systemCtx;
        Ctx = new RenderBuilderContext(systemCtx.Gfx, outputSize);
    }

    private void EnsureNotDone() => InvalidOpThrower.ThrowIf(IsDone, nameof(IsDone));

    internal RenderSetupPlan Build()
    {
        EnsureNotDone();
        InvalidOpThrower.ThrowIf(Ctx.Version == RenderPipelineVersion.None, nameof(Ctx.Version));
        InvalidOpThrower.ThrowIfAnyNull(Ctx.BatcherSetup, Ctx.CollectorSetup);
        InvalidOpThrower.ThrowIfAnyNull(Ctx.FboSetup, Ctx.ShaderProvider, Ctx.CoreShaderSetup);

        Ctx.Done = true;

        return Ctx.Compile();
    }

    public void SetupRegistry(Action<RenderRegistryBuilder> registryBuilder)
    {
        EnsureNotDone();
        ArgumentNullException.ThrowIfNull(registryBuilder, nameof(registryBuilder));
        registryBuilder(new RenderRegistryBuilder(SystemCtx.Registry, Ctx));
    }

    public void SetupBatchers(Action<GfxContext, BatcherRegistry> builder)
    {
        EnsureNotDone();
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        Ctx.BatcherSetup = builder;
    }

    public void SetupDrawPipeline(Action<IDrawCommandCollector> builder)
    {
        EnsureNotDone();
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        Ctx.CollectorSetup = builder;
    }

    public void SetupPassPipeline(RenderPipelineVersion version)
    {
        EnsureNotDone();
        ArgumentOutOfRangeException.ThrowIfEqual((int)version, (int)RenderPipelineVersion.None);
        Ctx.Version = version;
    }
}

public sealed class RenderRegistryBuilder
{
    private RenderBuilderContext Ctx { get; }
    private RenderRegistry RenderRegistry { get; }

    internal RenderRegistryBuilder(RenderRegistry renderRegistry, RenderBuilderContext ctx)
    {
        Ctx = ctx;
        RenderRegistry = renderRegistry;
    }

    public RenderRegistryBuilder RegisterFbo<TTag>(FboVariant variant, RegisterFboEntry entry)
        where TTag : unmanaged, IRenderPassTag
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(variant.Value, 0, nameof(variant));
        ArgumentNullException.ThrowIfNull(entry, nameof(entry));

        Ctx.FboSetup.Add((variant, entry, Action));
        return this;

        void Action(FboVariant v, RegisterFboEntry e) 
            => RenderRegistry.FboRegistry.Register<TTag>(v, e, Ctx.OutputSize);
    }

    public RenderRegistryBuilder RegisterShader(int shaderCount, Action<Span<ShaderId>> provider)
    {
        ArgumentNullException.ThrowIfNull(provider, nameof(provider));
        Ctx.ShaderCount = shaderCount;
        Ctx.ShaderProvider = provider;
        return this;
    }

    public RenderRegistryBuilder RegisterCoreShaders(Func<RenderCoreShaders> provider)
    {
        ArgumentNullException.ThrowIfNull(provider, nameof(provider));
        Ctx.CoreShaderSetup = provider;
        return this;
    }
}