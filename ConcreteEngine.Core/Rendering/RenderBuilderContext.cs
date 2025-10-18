using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Batching;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Core.Rendering.Descriptors;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Core.Rendering;

internal sealed record RenderSetupPlan(
    Size2D OutputSize,
    RenderPipelineVersion Version,
    Action<GfxContext, BatcherRegistry> BatcherSetup,
    Action<IDrawCommandCollector> CollectorSetup,
    List<(FboVariant, RegisterFboEntry, Action<FboVariant, RegisterFboEntry>)> FboSetup,
    Action<Span<ShaderId>> ShaderProvider,
    Func<RenderCoreShaders> CoreShaderSetup,
    int ShaderCount);

public sealed class RenderBuilderContext
{
    public GfxContext Gfx { get; private set; }
    public Size2D OutputSize { get; }
    public bool Done { get; internal set; }

    internal RenderPipelineVersion Version { get; set; } = RenderPipelineVersion.None;

    internal Action<GfxContext, BatcherRegistry>? BatcherSetup { get; set; }
    internal Action<IDrawCommandCollector>? CollectorSetup { get; set; }

    internal List<(FboVariant, RegisterFboEntry, Action<FboVariant, RegisterFboEntry>)> FboSetup { get; private set; } =
        new(8);

    internal int ShaderCount { get; set; }
    internal Action<Span<ShaderId>>? ShaderProvider { get; set; }

    internal Func<RenderCoreShaders>? CoreShaderSetup { get; set; }

    internal RenderBuilderContext(GfxContext gfx, Size2D outputSize)
    {
        Gfx = gfx;
        OutputSize = outputSize;
    }


    internal RenderSetupPlan Compile()
    {
        var res = new RenderSetupPlan(
            OutputSize, Version,
            BatcherSetup!, CollectorSetup!,
            FboSetup, ShaderProvider!, CoreShaderSetup!,
            ShaderCount
        );

        Reset();
        return res;
    }

    private void Reset()
    {
        Gfx = null!;
        BatcherSetup = null!;
        CollectorSetup = null!;
        FboSetup = null!;
        ShaderProvider = null!;
        CoreShaderSetup = null!;
    }
}