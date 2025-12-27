using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Descriptors;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Renderer;

internal sealed class RenderSetupPlan(
    Size2D outputSize,
    RenderPipelineVersion version,
    List<RenderSetupPlan.FboSetupRecord> fboSetup,
    Action<Span<ShaderId>> shaderProvider,
    Func<RenderCoreShaders> coreShaderSetup,
    int shaderCount)
{
    public readonly Size2D OutputSize = outputSize;
    public readonly RenderPipelineVersion Version = version;
    public readonly List<FboSetupRecord> FboSetup = fboSetup;
    public readonly Action<Span<ShaderId>> ShaderProvider = shaderProvider;
    public readonly Func<RenderCoreShaders> CoreShaderSetup = coreShaderSetup;
    public readonly int ShaderCount = shaderCount;

    public class FboSetupRecord(
        FboVariant variant,
        RegisterFboEntry entry,
        Action<FboVariant, RegisterFboEntry> registerFbo)
    {
        public readonly FboVariant Variant = variant;
        public readonly RegisterFboEntry Entry = entry;
        public readonly Action<FboVariant, RegisterFboEntry> RegisterFbo = registerFbo;
    }
}

public sealed class RenderBuilderContext
{
    public GfxContext Gfx { get; private set; }
    public Size2D OutputSize { get; }
    public bool Done { get; internal set; }

    internal RenderPipelineVersion Version { get; set; } = RenderPipelineVersion.None;

    internal List<RenderSetupPlan.FboSetupRecord> FboSetup { get; private set; } = new(8);

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