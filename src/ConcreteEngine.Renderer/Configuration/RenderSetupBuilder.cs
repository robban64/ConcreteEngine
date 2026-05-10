using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Descriptors;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Renderer.Configuration;

public sealed class RenderSetupBuilder
{
    private RenderProgramContext ProgramCtx { get; }
    private RenderBuilderContext Ctx { get; }

    public bool IsDone => Ctx.Done;

    internal RenderSetupBuilder(RenderProgramContext programCtx, Size2D outputSize)
    {
        ArgumentNullException.ThrowIfNull(programCtx);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Width, 4);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Height, 4);

        ProgramCtx = programCtx;
        Ctx = new RenderBuilderContext(programCtx.Gfx, outputSize);
    }

    private void EnsureNotDone() => InvalidOpThrower.ThrowIf(IsDone, nameof(IsDone));

    internal RenderBuilderContext Build()
    {
        EnsureNotDone();
        InvalidOpThrower.ThrowIf(Ctx.Version == RenderPipelineVersion.None, nameof(Ctx.Version));
        InvalidOpThrower.ThrowIfAnyNull(Ctx.FboSetup, Ctx.ShaderIds);

        Ctx.Done = true;
        return Ctx;
    }


    public RenderSetupBuilder RegisterFbo<TTag>(FboVariant variant, RegisterFboEntry entry)
        where TTag : class
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(variant.Value, 0, nameof(variant));
        ArgumentNullException.ThrowIfNull(entry, nameof(entry));

        Ctx.FboSetup.Add(new RenderBuilderContext.FboSetupRecord(variant, entry, Action));
        return this;

        void Action(FboVariant v, RegisterFboEntry e) =>
            ProgramCtx.Registry.FboRegistry.Register<TTag>(v, e, Ctx.OutputSize);
    }

    public RenderSetupBuilder RegisterShaders(ShaderId[] shaderIds, RenderCoreShaders coreShaders)
    {
        Ctx.CoreShaders = coreShaders;
        Ctx.ShaderIds = shaderIds;
        return this;
    }

    public void SetupPassPipeline(RenderPipelineVersion version)
    {
        EnsureNotDone();
        ArgumentOutOfRangeException.ThrowIfEqual((int)version, (int)RenderPipelineVersion.None);
        Ctx.Version = version;
    }
}