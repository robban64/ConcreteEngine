using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Engine.Systems;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render.Passes;

internal sealed class RenderPassProgram
{
    public RenderPassState State;
    public PassTargetKey PassKey { get; private set; }

    public readonly DrawCommandProcessor DrawCmd;

    private readonly RenderPassEntry[] _passEntries;

    internal RenderPassProgram(DrawCommandProcessor drawCmd, RenderPassEntry[] passEntries)
    {
        ArgumentNullException.ThrowIfNull(drawCmd);
        DrawCmd = drawCmd;
        _passEntries = passEntries;
    }
    
    public GfxCommands Gfx => DrawCmd.GfxCmd;
    public GfxBuffers GfxBuffers => DrawCmd.GfxBuffers;

    public FrameBufferId TargetFbo => State.Target;
    public ShaderId PassShader => State.PassShader;

    public ref readonly FrameBufferMeta TargetMeta
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref GfxRegistry.GetMeta(State.Target);
    }

    internal void Reset()
    {
        State = default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AttachScreenPass(PassTargetKey targetKey, RenderPassState state)
    {
        State = state;
        PassKey = targetKey;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AttachPass(FrameBufferId fboId, PassTargetKey targetKey, RenderPassState state)
    {
        State = state with { Target = fboId };
        PassKey = targetKey;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<TextureId> GetPassSources() => _passEntries[PassKey.Pass].GetSources();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SampleTo<TTarget>(FboVariant variant, byte slot, TextureId texture)
        where TTarget : unmanaged, IRenderTarget
    {
        Debug.Assert(slot < RenderLimits.TextureSlots);
        var key = RenderRegistry.TargetRegistry<TTarget>.PassKey(variant);
        _passEntries[key.Pass].SetSourceSlot(slot, texture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MutateStatePass<TTarget>(FboVariant variant, FrameBufferId targetFboId)
        where TTarget : unmanaged, IRenderTarget
    {
        var key = RenderRegistry.TargetRegistry<TTarget>.PassKey(variant);
        _passEntries[key.Pass].State.ResolveTarget = targetFboId;
    }


    //
    public void ActivateDepthMode()
    {
        RenderContext.ApplyForDepthPass();
        VisualSystem.Instance.UploadShadow();
        VisualSystem.Instance.UploadLightView();
    }

    public void RestoreMode()
    {
        RenderContext.ResetContext();
        VisualSystem.Instance.UploadMainView();
    }
}