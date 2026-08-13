using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Engine.Systems;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render.Passes;

internal readonly ref struct RenderPassContext(RenderPassEntry passEntry, DrawCommandProcessor drawCmd)
{
    private readonly RenderPassEntry _passEntry = passEntry;
    public readonly DrawCommandProcessor DrawCmd = drawCmd;

    public ref PassState State
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _passEntry.State;
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

    public FrameBufferId TargetFbo => State.Target;
    public ShaderId PassShader => State.PassShader;

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
        RenderRegistry.GetPassEntry<TTarget>(variant).SetSourceSlot(slot, texture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MutateStatePass<TTarget>(FboVariant variant, FrameBufferId targetFboId)
        where TTarget : unmanaged, IRenderTarget
    {
        RenderRegistry.GetPassEntry<TTarget>(variant).State.ResolveTarget = targetFboId;
    }
    
    public void DrawFullscreenQuad()
    {
        var sources = _passEntry.GetSources();
        
        Gfx.UseShader(PassShader);
        for (var i = 0; i < 4; ++i)
        {
            var source = sources[i];
            if (source > 0) Gfx.BindTextureAndSampler(source, SamplerProfile.PointClamp, (byte)i);
        }

        Gfx.DrawMesh(GfxMeshes.FsqQuad);
    }

}