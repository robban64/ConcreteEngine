using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Gfx.Resources.Handles;
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


    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareFrame(
        in RenderFrameInfo frameInfo,
        in RenderRuntimeParams runtimeParams)
    {
        Debug.Assert(Initialized);

        _stateContext.SetCurrentFrameInfo(in frameInfo, in runtimeParams);

        _passPipeline.Prepare(frameInfo.OutputSize);
        _drawPipeline.Prepare();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StartFrame(BeginFrameStatus status)
    {
        ref readonly var frameInfo = ref _stateContext.CurrentFrameInfo;
        _graphics.BeginFrame(frameInfo.ToGfxFrameInfo());

        if (status == BeginFrameStatus.Resize)
            _renderRegistry.FboRegistry.RecreateScreenDependentFbo(frameInfo.OutputSize);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndRenderFrame(out GfxFrameResult frameResult)
    {
        _graphics.EndFrame(out frameResult);
    }

    public void RenderEmptyFrame(RenderFrameInfo frameInfo)
    {
        _graphics.BeginFrame(frameInfo.ToGfxFrameInfo());
        _graphics.EndFrame(out _);
    }

    public void Shutdown()
    {
    }
}