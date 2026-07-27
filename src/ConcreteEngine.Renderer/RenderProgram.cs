using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Configuration;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer;

public sealed class RenderProgram : IDisposable
{
    private readonly DrawCommandPipeline _drawPipeline;
    private readonly RenderPassPipeline _passPipeline;

    private readonly RenderProgramContext _programContext;

    public readonly RenderRegistry Registry;
    public readonly RenderUploadBuffers UploadBuffers;

    public bool Initialized { get; private set; }

    public RenderProgram(GraphicsRuntime graphics, UniformUploaderCallbacks uploaderCallbacks)
    {
        RenderContext.Make(uploaderCallbacks);

        Registry = new RenderRegistry(graphics.Gfx);

        UploadBuffers = new RenderUploadBuffers();
        _drawPipeline = new DrawCommandPipeline(UploadBuffers);
        _passPipeline = new RenderPassPipeline(Registry);

        _programContext = new RenderProgramContext
        {
            CommandPipeline = _drawPipeline, Gfx = graphics.Gfx, Registry = Registry, PassPipeline = _passPipeline
        };
    }

    public int PassCount => _passPipeline.PassCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TextureId GetOutputTexture() => RenderContext.Instance.OutputTexture;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UniformUploadContext GetUploadContext() => _drawPipeline.UniformUploader.GetUploadContext();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CollectDrawBuffers() => _drawPipeline.PrepareDrawBuffers();

    //
    public void PrepareFrame()
    {
        Debug.Assert(Initialized);
        RenderContext.Instance.ResetPassMode();
        _passPipeline.Prepare();
        _drawPipeline.Prepare();
    }

    public void ResizeScreenFrameBuffers(Size2D outputSize)
    {
        var currentOutputSize = RenderContext.Instance.OutputSize;
        RenderContext.Instance.OutputSize = outputSize;

        if (outputSize != currentOutputSize)
            Registry.RecreateScreenDependentFbo(outputSize);
    }


    public void ResizeShadowFrameBuffers(int shadowSize)
    {
        var currentShadowSize = RenderContext.Instance.ShadowMapDimension;
        RenderContext.Instance.ShadowMapDimension = shadowSize;

        if (shadowSize != currentShadowSize)
            Registry.RecreateFixedFrameBuffer<ShadowPassTag>(FboVariant.V0, new Size2D(shadowSize));

    }

    public void Render()
    {
        _drawPipeline.UploadUniforms();

        while (_passPipeline.NextPass(out var nextPassId, out var passAction))
        {
            if (passAction == NextPassAction.Skip) continue;
            var passResult = _passPipeline.ApplyPass();
            
            switch (passResult.Op)
            {
                case PassOp.Draw:
                    _drawPipeline.ExecuteDrawPass(nextPassId, true);
                    break;
                case PassOp.DrawEffect:
                    _drawPipeline.ExecuteDrawPass(nextPassId, false);
                    break;
            }

            _passPipeline.ApplyAfterPass();
        }

        if (++ticks > 144)
        {
            ticks = 0;
            DrawCommandProcessor.avg.ResetAndPrint("Draw");
        }
    }

    private int ticks = 0;

    //
    
    public RenderSetupBuilder StartBuilder(Size2D outputSize)
    {
        return new RenderSetupBuilder(_programContext, outputSize);
    }

    public void ApplyBuilder(RenderSetupBuilder builder)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(builder.IsDone, true, nameof(builder.IsDone));

        var plan = builder.Build();

        // Registry setup
        Registry.BeginRegistration();

        // register FBO
        foreach (var it in plan.FboSetup)
            it.RegisterFbo(it.Variant, it.Entry);

        // Register Shaders
        Registry.FinishRegistration();

        _drawPipeline.Initialize(_programContext);
        _passPipeline.Initialize(_programContext);

        PassPipeline3D.RegisterPassPipeline(_passPipeline);
        Initialized = true;
    }

    public void Dispose()
    {
        UploadBuffers.Dispose();
    }
}