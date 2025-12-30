using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Draw;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Registry;
using ConcreteEngine.Renderer.State;

namespace ConcreteEngine.Renderer;

public sealed class RenderEngine
{
    private readonly GraphicsRuntime _graphics;

    private readonly RenderRegistry _renderRegistry;
    private readonly DrawCommandPipeline _drawPipeline;
    private readonly RenderPassPipeline _passPipeline;

    private readonly RenderCamera _renderCamera;

    private readonly RenderStateContext _stateContext;

    public bool Initialized { get; private set; }

    private RenderEngineContext EngineContext { get; }


    public RenderEngine(GraphicsRuntime graphics, MeshId fsqMesh)
    {
        _graphics = graphics;

        _renderCamera = new RenderCamera();

        _renderRegistry = new RenderRegistry(graphics.Gfx);
        _drawPipeline = new DrawCommandPipeline();
        _passPipeline = new RenderPassPipeline(_renderRegistry.FboRegistry);

        _stateContext = new RenderStateContext { Camera = _renderCamera, FsqMesh = fsqMesh };

        EngineContext = new RenderEngineContext
        {
            CommandPipeline = _drawPipeline,
            Gfx = graphics.Gfx,
            Registry = _renderRegistry,
            PassPipeline = _passPipeline
        };
    }

    public int PassCount => _passPipeline.PassCount;

    public DrawCommandBuffer CommandBuffer => _drawPipeline.CommandBuffer;
    public IRenderFboRegistry FboRegistry => _renderRegistry.FboRegistry;
    public RenderCamera RenderCamera => _renderCamera;


    public void SetRenderParams(RenderParamsSnapshot snapshot) => _stateContext.Snapshot = snapshot;

    public RenderSetupBuilder StartBuilder(Size2D outputSize) => new(EngineContext, outputSize);

    public void ApplyBuilder(RenderSetupBuilder builder)
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
        plan.ShaderProvider(shaderIds);
        var coreShaders = plan.CoreShaderSetup();
        _renderRegistry.ShaderRegistry.RegisterCollection(shaderIds);
        _renderRegistry.ShaderRegistry.RegisterCoreShader(in coreShaders);
        _renderRegistry.FinishRegistration();

        _drawPipeline.Initialize(EngineContext, _stateContext);
        _passPipeline.Initialize(EngineContext);

        PassPipeline3D.RegisterPassPipeline(_passPipeline, in _renderRegistry.ShaderRegistry.CoreShaders);
        Initialized = true;
    }

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SubmitMaterialDrawData(in RenderMaterialPayload payload, ReadOnlySpan<TextureSlotInfo> slots) =>
        _drawPipeline.SubmitMaterialDrawData(in payload, slots);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CollectDrawBuffers() => _drawPipeline.PrepareDrawBuffers();


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareFrame(in FrameInfo frameInfo, in RenderRuntimeParams runtimeParams)
    {
        Debug.Assert(Initialized);

        _stateContext.SetCurrentFrameInfo(in frameInfo, in runtimeParams);

        _passPipeline.Prepare(frameInfo.OutputSize);
        _drawPipeline.Prepare();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadFrameData()
    {
        _drawPipeline.UploadUniformGlobals();
        _drawPipeline.UploadDrawUniformData();
    }

    private DurationProfileTimer _timer = new(TimeSpan.FromSeconds(4), "\n\nNextPass");
    private DurationProfileTimer _timer2 = new(TimeSpan.FromSeconds(4), "ApplyPass");
    private DurationProfileTimer _timer3 = new(TimeSpan.FromSeconds(4), "AfterPass");

    public void Render()
    {
        while (true)
        {
            _timer.Begin();
            var nextPass = _passPipeline.NextPass(out var nextPassRes);
            _timer.EndPrint();
            if(!nextPass) break;
            if (nextPassRes.ActionKind == PreparePassActionKind.Skip) continue;
            ExecutePass(nextPassRes.PassId);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecutePass(PassId passId)
    {
        _timer2.Begin();
        var passResult = _passPipeline.ApplyPass();
        _timer2.EndPrint();
        
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

        _timer3.Begin();
        _passPipeline.ApplyAfterPass();
        _timer3.EndPrint();
    }

}