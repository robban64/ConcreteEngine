using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Draw;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Registry;
using ConcreteEngine.Renderer.State;

namespace ConcreteEngine.Renderer;

public sealed class RenderProgram
{
    private readonly GraphicsRuntime _graphics;

    private readonly RenderRegistry _renderRegistry;
    private readonly DrawCommandPipeline _drawPipeline;
    private readonly RenderPassPipeline _passPipeline;

    private readonly RenderStateContext _stateContext;

    private readonly RenderProgramContext _programContext;

    public RenderCamera RenderCamera { get; }

    public bool Initialized { get; private set; }

    public RenderProgram(GraphicsRuntime graphics, MeshId fsqMesh)
    {
        _graphics = graphics;
        RenderCamera = new RenderCamera();

        _renderRegistry = new RenderRegistry(graphics.Gfx);
        _drawPipeline = new DrawCommandPipeline();
        _passPipeline = new RenderPassPipeline(_renderRegistry.FboRegistry);

        _stateContext = new RenderStateContext { Camera = RenderCamera, FsqMesh = fsqMesh };

        _programContext = new RenderProgramContext
        {
            CommandPipeline = _drawPipeline,
            Gfx = graphics.Gfx,
            Registry = _renderRegistry,
            PassPipeline = _passPipeline
        };
    }

    public int PassCount => _passPipeline.PassCount;

    public DrawCommandBuffer CommandBuffer => _drawPipeline.CommandBuffer;
    public RenderRegistry Registry => _renderRegistry;

    public RenderParamsSnapshot GetRenderParams() => _stateContext.Snapshot;

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SubmitMaterialDrawData(in RenderMaterialPayload payload, ReadOnlySpan<TextureSlotInfo> slots) =>
        _drawPipeline.SubmitMaterialDrawData(in payload, slots);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CollectDrawBuffers() => _drawPipeline.PrepareDrawBuffers();


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareFrame(in RenderFrameArgs args)
    {
        Debug.Assert(Initialized);
        var snapshot = _stateContext.Snapshot;

        if (snapshot.WasDirty) snapshot.WasDirty = false;
        if (snapshot.IsDirty)
        {
            var fboRegistry = _renderRegistry.FboRegistry;
            var outputSize = snapshot.ScreenFboSize;
            var shadowSize = snapshot.Shadow.ShadowMapSize;
            snapshot.IsDirty = false;
            snapshot.WasDirty = true;
            
            if (outputSize != fboRegistry.OutputSize)
                fboRegistry.RecreateScreenDependentFbo(outputSize);

            if (shadowSize != fboRegistry.ShadowMapSize.Width)
                fboRegistry.RecreateFixedFrameBuffer<ShadowPassTag>(FboVariant.Default, new Size2D(shadowSize));
        }

        _stateContext.RenderFrameArgs = args;

        _passPipeline.Prepare(args.OutputSize);
        _drawPipeline.Prepare();
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
            case PassOpKind.Resolve:
                _passPipeline.ApplyAfterPass();
                return;
        }

        _passPipeline.ApplyAfterPass();
    }

    //
    public RenderSetupBuilder StartBuilder(Size2D windowSize, Size2D outputSize)
    {
        _stateContext.RenderFrameArgs = new RenderFrameArgs { OutputSize = outputSize };
        return new RenderSetupBuilder(_programContext, outputSize);
    }

    public void ApplyBuilder(object provider, RenderSetupBuilder builder)
    {
        InvalidOpThrower.ThrowIf(builder.IsDone, nameof(builder.IsDone));

        var plan = builder.Build();

        // Registry setup
        _renderRegistry.BeginRegistration(plan.OutputSize);

        // register FBO
        foreach (var it in plan.FboSetup)
            it.RegisterFbo(it.Variant, it.Entry);

        // Register Shaders
        Span<ShaderId> shaderIds = stackalloc ShaderId[plan.ShaderCount];
        plan.ShaderProvider(provider, shaderIds);
        var coreShaders = plan.CoreShaderSetup(provider);
        _renderRegistry.ShaderRegistry.RegisterCollection(shaderIds);
        _renderRegistry.ShaderRegistry.RegisterCoreShader(in coreShaders);
        _renderRegistry.FinishRegistration();

        _drawPipeline.Initialize(_programContext, _stateContext);
        _passPipeline.Initialize(_programContext);

        PassPipeline3D.RegisterPassPipeline(_passPipeline, in _renderRegistry.ShaderRegistry.CoreShaders);
        Initialized = true;
    }

    public void PrepareFrameWarmup(Size2D windowSize, Size2D outputSize)
    {
        _stateContext.RenderFrameArgs = new RenderFrameArgs { OutputSize = outputSize };
        _passPipeline.Prepare(outputSize);
        _drawPipeline.Prepare();
    }
}