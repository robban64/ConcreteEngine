using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Configuration;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer;

public sealed class RenderProgram
{
    private readonly DrawCommandPipeline _drawPipeline;
    private readonly RenderPassPipeline _passPipeline;

    private readonly RenderProgramContext _programContext;

    public readonly RenderRegistry Registry;
    public readonly RenderUploadBuffers UploadBuffers;

    public bool Initialized { get; private set; }

    public RenderProgram(GraphicsRuntime graphics, CameraTransforms camera, VisualEnvironment visualEnvironment)
    {
        VisualRenderContext.Make(camera, visualEnvironment);

        Registry = new RenderRegistry(graphics.Gfx);

        UploadBuffers = new RenderUploadBuffers();
        _drawPipeline = new DrawCommandPipeline(UploadBuffers);
        _passPipeline = new RenderPassPipeline(Registry.FboRegistry);

        _programContext = new RenderProgramContext
        {
            CommandPipeline = _drawPipeline, Gfx = graphics.Gfx, Registry = Registry, PassPipeline = _passPipeline
        };
    }

    public int PassCount => _passPipeline.PassCount;
    public TextureId OutputTexture => VisualRenderContext.Instance.OutputTexture;
    public UniformUploader UniformUploader => _drawPipeline.UniformUploader;

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CollectDrawBuffers() => _drawPipeline.PrepareDrawBuffers();


    public void PrepareFrame(float dt, Size2D outputSize)
    {
        Debug.Assert(Initialized);
        var visualCtx = VisualRenderContext.Instance;
        visualCtx.DeltaTime = dt;
        visualCtx.OutputSize = outputSize;

        if (visualCtx.Environment.WasDirty)
        {
            var fboRegistry = Registry.FboRegistry;
            var fboOutputSize = visualCtx.Environment.ScreenFboSize;
            var shadowSize = visualCtx.Environment.GetShadow().ShadowMapSize;

            if (fboOutputSize != fboRegistry.OutputSize)
                fboRegistry.RecreateScreenDependentFbo(fboOutputSize);

            if (shadowSize != fboRegistry.ShadowMapSize.Width)
                fboRegistry.RecreateFixedFrameBuffer<ShadowPassTag>(FboVariant.V0, new Size2D(shadowSize));
        }

        _passPipeline.Prepare();
        _drawPipeline.Prepare();
    }


    public void UploadFrameData(RenderFrameArgs frameArgs)
    {
        _drawPipeline.UploadUniformGlobals(in frameArgs);
        _drawPipeline.UploadDrawUniformData();
    }

    public void Render()
    {
        while (_passPipeline.NextPass(out var nextPassRes))
        {
            if (nextPassRes.ActionKind == PreparePassActionKind.Skip) continue;
            ExecutePass(nextPassRes.PassId);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecutePass(PassId passId)
    {
        var passResult = _passPipeline.ApplyPass();

        switch (passResult.Op)
        {
            case PassOp.Draw:
                _drawPipeline.ExecuteDrawPass(passId, true);
                break;
            case PassOp.DrawEffect:
                _drawPipeline.ExecuteDrawPass(passId, false);
                break;
        }

        _passPipeline.ApplyAfterPass();
    }

    //
    public RenderSetupBuilder StartBuilder(Size2D outputSize)
    {
        return new RenderSetupBuilder(_programContext, outputSize);
    }

    public void ApplyBuilder(RenderSetupBuilder builder)
    {
        InvalidOpThrower.ThrowIf(builder.IsDone, nameof(builder.IsDone));

        var plan = builder.Build();

        // Registry setup
        Registry.BeginRegistration(plan.OutputSize);

        // register FBO
        foreach (var it in plan.FboSetup)
            it.RegisterFbo(it.Variant, it.Entry);

        // Register Shaders
        Registry.ShaderRegistry.RegisterCollection(plan.ShaderIds);
        Registry.ShaderRegistry.RegisterCoreShader(in plan.CoreShaders);
        Registry.FinishRegistration();

        _drawPipeline.Initialize(_programContext);
        _passPipeline.Initialize(_programContext);

        PassPipeline3D.RegisterPassPipeline(_passPipeline, in Registry.ShaderRegistry.CoreShaders);
        Initialized = true;
    }
}