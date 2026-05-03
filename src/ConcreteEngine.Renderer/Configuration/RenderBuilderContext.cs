using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Descriptors;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Renderer.Configuration;

internal sealed class RenderSetupPlan(
    Size2D outputSize,
    RenderPipelineVersion version,
    List<RenderSetupPlan.FboSetupRecord> fboSetup,
    ShaderId[] shaderIds,
    in RenderCoreShaders coreShaders)
{
    public readonly Size2D OutputSize = outputSize;
    public readonly RenderPipelineVersion Version = version;
    public readonly List<FboSetupRecord> FboSetup = fboSetup;
    public readonly ShaderId[] ShaderIds = shaderIds;
    public readonly RenderCoreShaders CoreShaders = coreShaders;

    public sealed class FboSetupRecord(
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
    internal ShaderId[]? ShaderIds { get;  set; }

    internal RenderCoreShaders CoreShaders;

    internal RenderBuilderContext(GfxContext gfx, Size2D outputSize)
    {
        Gfx = gfx;
        OutputSize = outputSize;
    }

    internal RenderSetupPlan Compile()
    {
        var res = new RenderSetupPlan(OutputSize, Version, FboSetup, ShaderIds!, in CoreShaders);

        Reset();
        return res;
    }

    private void Reset()
    {
        Gfx = null!;
        FboSetup = null!;
        ShaderIds = null!;
    }
}