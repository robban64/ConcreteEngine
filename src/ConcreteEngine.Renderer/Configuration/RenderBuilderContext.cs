using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Descriptors;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Renderer.Configuration;

public sealed class RenderBuilderContext
{
    public sealed class FboSetupRecord(
        FboVariant variant,
        RegisterFboEntry entry,
        Action<FboVariant, RegisterFboEntry> registerFbo)
    {
        public readonly FboVariant Variant = variant;
        public readonly RegisterFboEntry Entry = entry;
        public readonly Action<FboVariant, RegisterFboEntry> RegisterFbo = registerFbo;
    }


    public GfxContext Gfx { get; private set; }
    public Size2D OutputSize { get; }
    public bool Done { get; internal set; }
    internal RenderPipelineVersion Version { get; set; } = RenderPipelineVersion.None;
    internal List<FboSetupRecord> FboSetup { get; private set; } = new(8);
    internal ShaderId[] ShaderIds { get; set; } = [];

    internal RenderCoreShaders CoreShaders;

    internal RenderBuilderContext(GfxContext gfx, Size2D outputSize)
    {
        Gfx = gfx;
        OutputSize = outputSize;
    }
}