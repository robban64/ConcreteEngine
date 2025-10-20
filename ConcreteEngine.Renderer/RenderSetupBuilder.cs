#region

using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Descriptors;
using ConcreteEngine.Renderer.Passes;

#endregion

namespace ConcreteEngine.Renderer;

public sealed class RenderSetupBuilder
{
    private RenderEngineContext EngineCtx { get; }
    private RenderBuilderContext Ctx { get; }

    public bool IsDone => Ctx.Done;

    internal RenderSetupBuilder(RenderEngineContext engineCtx, Size2D outputSize)
    {
        ArgumentNullException.ThrowIfNull(engineCtx);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Width, 4);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Height, 4);

        EngineCtx = engineCtx;
        Ctx = new RenderBuilderContext(engineCtx.Gfx, outputSize);
    }

    private void EnsureNotDone() => InvalidOpThrower.ThrowIf(IsDone, nameof(IsDone));

    internal RenderSetupPlan Build()
    {
        EnsureNotDone();
        InvalidOpThrower.ThrowIf(Ctx.Version == RenderPipelineVersion.None, nameof(Ctx.Version));
        InvalidOpThrower.ThrowIfAnyNull(Ctx.FboSetup, Ctx.ShaderProvider, Ctx.CoreShaderSetup);

        Ctx.Done = true;

        return Ctx.Compile();
    }


    public RenderSetupBuilder RegisterFbo<TTag>(FboVariant variant, RegisterFboEntry entry)
        where TTag : unmanaged, IRenderPassTag
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(variant.Value, 0, nameof(variant));
        ArgumentNullException.ThrowIfNull(entry, nameof(entry));

        Ctx.FboSetup.Add((variant, entry, Action));
        return this;

        void Action(FboVariant v, RegisterFboEntry e)
            => EngineCtx.Registry.FboRegistry.Register<TTag>(v, e, Ctx.OutputSize);
    }

    public RenderSetupBuilder RegisterShader(int shaderCount, Action<Span<ShaderId>> provider)
    {
        ArgumentNullException.ThrowIfNull(provider, nameof(provider));
        Ctx.ShaderCount = shaderCount;
        Ctx.ShaderProvider = provider;
        return this;
    }

    public RenderSetupBuilder RegisterCoreShaders(Func<RenderCoreShaders> provider)
    {
        ArgumentNullException.ThrowIfNull(provider, nameof(provider));
        Ctx.CoreShaderSetup = provider;
        return this;
    }

    public void SetupPassPipeline(RenderPipelineVersion version)
    {
        EnsureNotDone();
        ArgumentOutOfRangeException.ThrowIfEqual((int)version, (int)RenderPipelineVersion.None);
        Ctx.Version = version;
    }
}