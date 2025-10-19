#region

using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Engine.RenderingSystem.Batching;
using ConcreteEngine.Core.Engine.RenderingSystem.Producers;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Core.Rendering.Draw;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Resources;
using RenderFrameInfo = ConcreteEngine.Core.Rendering.State.RenderFrameInfo;

#endregion

namespace ConcreteEngine.Core.Rendering;

public enum RenderType
{
    Render2D,
    Render3D
}

public sealed class RenderEngine
{
    private readonly GraphicsRuntime _graphics;

    private readonly RenderRegistry _renderRegistry;
    private readonly DrawCommandPipeline _drawPipeline;
    private readonly RenderPassPipeline _passPipeline;

    private readonly RenderView _renderView;

    private RenderEngineContext EngineContext { get; }
    private readonly RenderStateContext _stateContext;

    public bool Initialized { get; private set; } = false;

    public DrawCommandBuffer CommandBuffer => _drawPipeline.CommandBuffer;

    internal RenderEngine(GraphicsRuntime graphics, BatcherRegistry batches,  RenderSceneSnapshot sceneSnapshot)
    {
        _graphics = graphics;
        
        _renderView = new RenderView();

        _renderRegistry = new RenderRegistry(graphics.Gfx);
        _drawPipeline = new DrawCommandPipeline();
        _passPipeline = new RenderPassPipeline();

        _stateContext = new RenderStateContext { View = _renderView, Snapshot = sceneSnapshot };

        EngineContext = new RenderEngineContext
        {
            Batchers = batches,
            CommandPipeline = _drawPipeline,
            Gfx = graphics.Gfx,
            Registry = _renderRegistry,
            PassPipeline = _passPipeline,
        };
    }

    public RenderSetupBuilder StartBuilder(Size2D outputSize) => new(EngineContext, outputSize);

    public void ApplyBuilder(RenderSetupBuilder builder)
    {
        InvalidOpThrower.ThrowIf(builder.IsDone, nameof(builder.IsDone));

        var plan = builder.Build();

        // Registry setup
        _renderRegistry.BeginRegistration(plan.OutputSize);

        // register FBO
        foreach (var (variant, entry, registerFbo) in plan.FboSetup)
            registerFbo(variant, entry);

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
    internal void RenderEmptyFrame(in RenderFrameInfo frameInfo)
    {
        _graphics.BeginFrame(frameInfo.ToGfxFrameInfo());
        _graphics.EndFrame(out _);
    }

    public void SubmitMaterialDrawData(in DrawMaterialPayload payload, ReadOnlySpan<TextureSlotInfo> slots) =>
        _drawPipeline.SubmitMaterialDrawData(in payload, slots);


    internal void PrepareFrame(
        in RenderFrameInfo frameInfo,
        in RenderRuntimeParams runtimeParams,
        in RenderViewSnapshot viewSnapshot)
    {
        Debug.Assert(Initialized);

        _stateContext.SetCurrentFrameInfo(in frameInfo, in runtimeParams);

        _renderView.PrepareFrame(in viewSnapshot);

        _passPipeline.Prepare(frameInfo.OutputSize);
        _drawPipeline.Prepare();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FillDrawBuffers() => _drawPipeline.PrepareDrawBuffers();

    internal void StartFrame(BeginFrameStatus status)
    {
        ref readonly var frameInfo = ref _stateContext.CurrentFrameInfo;
        _graphics.BeginFrame(frameInfo.ToGfxFrameInfo());

        if (status == BeginFrameStatus.Resize)
            _renderRegistry.FboRegistry.RecreateSizedFrameBuffer(frameInfo.OutputSize);
    }

    public void UploadFrameData()
    {
        _drawPipeline.UploadUniformGlobals();
        _drawPipeline.UploadDrawUniformData();
    }

    internal void Render()
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

        if (passResult.OpKind == PassOpKind.Resolve)
        {
            _passPipeline.ApplyAfterPass();
            return;
        }

        if (passResult == PassAction.DrawPassResult())
        {
            _drawPipeline.ExecuteDrawPass(passId);
        }

        _passPipeline.ApplyAfterPass();
    }

    internal void EndRenderFrame(out GfxFrameResult frameResult)
    {
        _graphics.EndFrame(out frameResult);
    }

    public void Shutdown()
    {
    }
}