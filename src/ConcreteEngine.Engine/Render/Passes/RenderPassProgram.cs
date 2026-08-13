using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Systems;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render.Passes;

internal sealed class RenderPassProgram
{
    public FrameBufferId FboId { get; private set; }
    public PassTargetKey CurrentPassKey { get; private set; }

    public readonly DrawCommandProcessor DrawCmd;

    private int _sourceSlotHigh;
    private int _sourceCount;
    private int _mutationCount;

    private readonly TextureId[] _textureSlots;
    private readonly (TextureId Texture, PassTextureSlotKey Key)[] _sourceQueue;
    private readonly (PassMutationState State, PassTargetKey Key)[] _mutationQueue;

    internal RenderPassProgram(DrawCommandProcessor drawCmd)
    {
        ArgumentNullException.ThrowIfNull(drawCmd);
        DrawCmd = drawCmd;
        _sourceQueue = new (TextureId, PassTextureSlotKey)[8];
        _mutationQueue = new (PassMutationState State, PassTargetKey Key)[8];
        _textureSlots = new TextureId[RenderLimits.TextureSlots];
    }

    public GfxCommands Gfx => DrawCmd.GfxCmd;
    public GfxBuffers GfxBuffers => DrawCmd.GfxBuffers;

    public ref readonly FrameBufferMeta Target
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref GfxRegistry.GetMeta(FboId);
    }

    internal void Reset()
    {
        FboId = default;
        _sourceSlotHigh = 0;
        _sourceCount = 0;
        _mutationCount = 0;
        Array.Clear(_sourceQueue);
        Array.Clear(_mutationQueue);
        Array.Clear(_textureSlots);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AttachScreenPass(PassTargetKey targetKey)
    {
        FboId = default;
        CurrentPassKey = targetKey;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AttachPass(FrameBufferId fboId, PassTargetKey targetKey)
    {
        FboId = fboId;
        CurrentPassKey = targetKey;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<TextureId> GetPassSources() => new(_textureSlots, 0, int.Max(_sourceSlotHigh, 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SampleTo<TTarget>(FboVariant variant, byte slot, TextureId texture)
        where TTarget : unmanaged, IRenderTarget
    {
        Debug.Assert(slot < RenderLimits.TextureSlots);
        var passKey = RenderRegistry.TargetRegistry<TTarget>.PassKey(variant);
        var key = new PassTextureSlotKey(passKey.TagIndex, passKey.Variant, passKey.Pass, slot);
        _sourceQueue[_sourceCount++] = (texture, key);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MutateStatePass<TTarget>(FboVariant variant, in PassMutationState newState)
        where TTarget : unmanaged, IRenderTarget
    {
        var key = RenderRegistry.TargetRegistry<TTarget>.PassKey(variant);
        _mutationQueue[_mutationCount++] = (newState, key);
    }

    public void DequeueMutationTo(RenderPassEntry entry)
    {
        var key = entry.PassKey.TagIndex;
        var span = _mutationQueue.AsSpan(0, _mutationCount);
        span.Sort();
        foreach (ref readonly var it in span)
        {
            if (it.Key.TagIndex == key)
            {
                entry.UpdateState(in it.State);
            }
        }
    }

    public void DequeuePassSources(PassTargetKey key)
    {
        if(_sourceCount == 0) return;
        
        Array.Clear(_textureSlots);

        var textureSlotHigh = 0;

        var span = _sourceQueue.AsSpan(0, _sourceCount);
        span.Sort();
        
        foreach (var it in span)
        {
            if (it.Key.TagIndex == key.TagIndex)
            {
                _textureSlots[it.Key.TextureSlot] = it.Texture;
                textureSlotHigh = int.Max(textureSlotHigh, it.Key.TextureSlot);
            }
        }

        _sourceSlotHigh = textureSlotHigh;

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