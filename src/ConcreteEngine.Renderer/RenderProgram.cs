using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Configuration;
using ConcreteEngine.Renderer.Data;
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
            CommandPipeline = _drawPipeline,
            Gfx = graphics.Gfx,
            Registry = Registry,
            PassPipeline = _passPipeline
        };
    }

    public int PassCount => _passPipeline.PassCount;


    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CollectDrawBuffers() => _drawPipeline.PrepareDrawBuffers();


    public ref RenderFrameArgs PrepareFrame(Size2D outputSize)
    {
        Debug.Assert(Initialized);
        var visualCtx = VisualRenderContext.Instance;
        visualCtx.OutputSize = outputSize;

        if (visualCtx.Environment.WasDirty)
        {
            var fboRegistry = Registry.FboRegistry;
            var fboOutputSize = visualCtx.Environment.ScreenFboSize;
            var shadowSize = visualCtx.Environment.GetShadow().ShadowMapSize;

            if (fboOutputSize != fboRegistry.OutputSize)
                fboRegistry.RecreateScreenDependentFbo(fboOutputSize);

            if (shadowSize != fboRegistry.ShadowMapSize.Width)
                fboRegistry.RecreateFixedFrameBuffer<ShadowPassTag>(FboVariant.Default, new Size2D(shadowSize));
        }

        _passPipeline.Prepare();
        _drawPipeline.Prepare();
        return ref visualCtx.RenderFrameArgs;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadFrameData()
    {
        _drawPipeline.UploadUniformGlobals();
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

        switch (passResult.OpKind)
        {
            case PassOpKind.Draw:
                _drawPipeline.ExecuteDrawPass(passId, true);
                break;
            case PassOpKind.DrawEffect:
                _drawPipeline.ExecuteDrawPass(passId, false);
                break;
        }

        _passPipeline.ApplyAfterPass();
    }

    //
    public RenderSetupBuilder StartBuilder(Size2D windowSize, Size2D outputSize)
    {
        //VisualRenderContext.Instance.RenderFrameArgs = new RenderFrameArgs { OutputSize = outputSize };
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