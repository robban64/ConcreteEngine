using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Engine.Systems;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render.Passes;

internal sealed class RenderPassContext(DrawCommandProcessor drawCmd)
{
    public PassId CurrentPass { get; private set; }
    private PassState _passState;
    private GfxPassState _gfxPassState;

    private readonly FrameBufferId[] _targets = new FrameBufferId[RenderLimits.FboSlots];
    private readonly InlineArray4<TextureId>[] _sources = new InlineArray4<TextureId>[RenderLimits.FboSlots];

    public readonly DrawCommandProcessor DrawCmd = drawCmd;

    public ref PassState State => ref _passState;
    public ref GfxPassState GfxState => ref _gfxPassState;
    public FrameBufferId ResolveTarget => _passState.ResolveTarget;
    public FrameBufferId TargetFbo => _passState.Target;
    public ShaderId PassShader => _passState.PassShader;
    public bool LinearFilter => _passState.LinearFilter;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetFrame()
    {
        CurrentPass = default;
        _passState = default;
        _gfxPassState = default;
        Array.Clear(_targets);
        Array.Clear(_sources);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AttachPass(RenderPassEntry passEntry)
    {
        var passId = CurrentPass = passEntry.PassKey;
        _gfxPassState = passEntry.GfxState;
        _passState = passEntry.State with { ResolveTarget = _targets[passId] };
    }


    public GfxCommands Gfx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => DrawCmd.GfxCmd;
    }

    public GfxBuffers GfxBuffers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => DrawCmd.GfxBuffers;
    }

    public ref readonly FrameBufferMeta TargetMeta
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref GfxRegistry.GetMeta(State.Target);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SampleTo<TTarget>(FboVariant variant, byte slot, TextureId texture)
        where TTarget : unmanaged, IRenderTarget
    {
        Debug.Assert(slot < RenderLimits.TextureSlots);
        var toPassId = RenderRegistry.TargetRegistry<TTarget>.GetPassId(variant);
        _sources[toPassId][slot] = texture;
        //RenderRegistry.GetPassEntry<TTarget>(variant).SetSourceSlot(slot, texture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MutateStatePass<TTarget>(FboVariant variant, FrameBufferId targetFboId)
        where TTarget : unmanaged, IRenderTarget
    {
        var toPassId = RenderRegistry.TargetRegistry<TTarget>.GetPassId(variant);
        _targets[toPassId] = targetFboId;
        //RenderRegistry.GetPassEntry<TTarget>(variant).State.ResolveTarget = targetFboId;
    }


    public void DrawFullscreenQuad()
    {
        var gfx = Gfx;
        gfx.BeginRenderPass(TargetFbo, _gfxPassState);
        gfx.UseShader(PassShader);

        var sources = _sources[CurrentPass];
        for (var i = 0; i < 4; ++i)
        {
            var source = sources[i];
            gfx.BindTextureAndSampler(source, SamplerProfile.PointClamp, (byte)i);
        }

        gfx.DrawMesh(GfxMeshes.FsqQuad);
        gfx.EndRenderPass();
    }
}