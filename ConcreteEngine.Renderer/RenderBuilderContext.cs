#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Descriptors;
using ConcreteEngine.Renderer.Passes;

#endregion

namespace ConcreteEngine.Renderer;

internal sealed record RenderSetupPlan(
    Size2D OutputSize,
    RenderPipelineVersion Version,
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
        var res = new RenderSetupPlan(OutputSize, Version, FboSetup, ShaderProvider!, CoreShaderSetup!, ShaderCount);

        Reset();
        return res;
    }

    private void Reset()
    {
        Gfx = null!;
        FboSetup = null!;
        ShaderProvider = null!;
        CoreShaderSetup = null!;
    }
}