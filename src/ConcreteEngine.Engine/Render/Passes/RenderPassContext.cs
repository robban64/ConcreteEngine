using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Engine.Systems;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render.Passes;

internal sealed class RenderPassContext(DrawCommandProcessor drawCmd)
{
    public readonly DrawCommandProcessor DrawCmd = drawCmd;
    
    public PassId CurrentPass { get; private set; }
    private RenderPassParams _passParams;
    private GfxPassState _gfxState;

    private readonly PassData[] _passData = new PassData[RenderLimits.FboSlots];


    public ref readonly RenderPassParams Params => ref _passParams;
    public ref readonly GfxPassState GfxState => ref _gfxState;
    public FrameBufferId ResolveTarget => _passParams.ResolveTarget;
    public FrameBufferId TargetFbo => _passParams.Target;
    public ShaderId PassShader => _passParams.PassShader;
    public bool LinearFilter => _passParams.LinearFilter;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetFrame()
    {
        CurrentPass = default;
        _passParams = default;
        _gfxState = default;
        Array.Clear(_passData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AttachPass(RenderPassEntry passEntry)
    {
        var passId = CurrentPass = passEntry.PassKey;
        _gfxState = passEntry.GfxState;
        _passParams = passEntry.Params with { ResolveTarget = _passData[passId].ResolveTarget };
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
        get => ref GfxRegistry.GetMeta(Params.Target);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SampleTo<TTarget>(FboVariant v, byte slot, TextureId texture) where TTarget : unmanaged, IRenderTarget
    {
        Debug.Assert(slot < PassData.SlotLimit);
        var toPassId = RenderRegistry.TargetRegistry<TTarget>.GetPassId(v);
        _passData[toPassId].SetSlot(slot, texture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MutatePass<TTarget>(FboVariant v, FrameBufferId target) where TTarget : unmanaged, IRenderTarget
    {
        var toPassId = RenderRegistry.TargetRegistry<TTarget>.GetPassId(v);
        _passData[toPassId].ResolveTarget = target;
    }

    public void ApplyFsqSamplerBindings()
    {
        var gfx = Gfx;
        gfx.UnbindAllTextures();
        gfx.BindSampler(SamplerProfile.PointClamp, 0);
        gfx.BindSampler(SamplerProfile.PointClamp, 1);
        gfx.BindSampler(SamplerProfile.PointClamp, 2);
    }

    public void RunFsqPass()
    {
        var gfx = Gfx;
        gfx.BeginRenderPass(TargetFbo, _gfxState);
        gfx.UseShader(PassShader);

        var sources = _passData[CurrentPass];
        gfx.BindTextureSlot(sources.Slot0, 0);
        gfx.BindTextureSlot(sources.Slot1, 1);
        gfx.BindTextureSlot(sources.Slot2, 2);

        gfx.DrawMesh(GfxMeshes.FsqQuad);
        gfx.EndRenderPass();
    }

    private struct PassData
    {
        public const int SlotLimit = 3;
        
        public FrameBufferId ResolveTarget;
        public TextureId Slot0;
        public TextureId Slot1;
        public TextureId Slot2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetSlot(int slot, TextureId texture)
        {
            if (slot == 0) Slot0 = texture;
            else if (slot == 1) Slot1 = texture;
            else Slot2 = texture;
        }
    }
}