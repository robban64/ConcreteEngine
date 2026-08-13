using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Systems;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render.Passes;

internal sealed class RenderPassContext
{
    private int _textureSlotHigh;

    public FrameBufferId FboId { get; private set; }
    public PassTargetKey CurrentPassKey { get; private set; }

    public readonly DrawCommandProcessor DrawCmdProcessor;

    private readonly TextureId[] _textureSlots;
    private readonly PriorityQueue<TextureId, PassTextureSlotKey> _sourceQueue;
    private readonly PriorityQueue<PassMutationState, PassTargetKey> _mutationQueue;

    internal RenderPassContext(DrawCommandProcessor drawCmdProcessor)
    {
        ArgumentNullException.ThrowIfNull(drawCmdProcessor);
        DrawCmdProcessor = drawCmdProcessor;
        _sourceQueue = new PriorityQueue<TextureId, PassTextureSlotKey>(4, new PassTextureSlotKeyComp());
        _mutationQueue = new PriorityQueue<PassMutationState, PassTargetKey>(4, new PassTagKeyComp());
        _textureSlots = new TextureId[RenderLimits.TextureSlots];
    }

    public GfxCommands Cmd => DrawCmdProcessor.GfxCmd;
    public GfxBuffers Buffers => DrawCmdProcessor.GfxBuffers;

    public ref readonly FrameBufferMeta Target
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref GfxRegistry.GetMeta(FboId);
    }

    internal void Reset()
    {
        FboId = default;
        _textureSlotHigh = 0;
        _sourceQueue.Clear();
        _mutationQueue.Clear();
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
    public ReadOnlySpan<TextureId> GetPassSources() => new(_textureSlots, 0, int.Max(_textureSlotHigh, 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SampleTo<TTarget>(FboVariant variant, byte slot, TextureId texture)
        where TTarget : unmanaged, IRenderTarget
    {
        Debug.Assert(slot < RenderLimits.TextureSlots);
        var passKey = RenderRegistry.TargetRegistry<TTarget>.PassKey(variant);
        var key = new PassTextureSlotKey(passKey.TagIndex, passKey.Variant, passKey.Pass, slot);
        _sourceQueue.Enqueue(texture, key);
    }

    public void MutateStatePass<TTarget>(FboVariant variant, in PassMutationState newState)
        where TTarget : unmanaged, IRenderTarget
    {
        var key = RenderRegistry.TargetRegistry<TTarget>.PassKey(variant);
        _mutationQueue.Enqueue(newState, key);
    }

    public void DequeueMutationTo(RenderPassEntry entry)
    {
        while (_mutationQueue.TryPeek(out _, out var k) && k.TagIndex == entry.PassKey.TagIndex)
        {
            _mutationQueue.TryDequeue(out var state, out k);
            entry.UpdateState(state);
        }
    }

    public void DequeuePassSources(RenderPassEntry entry)
    {
        var tagIndex = entry.PassKey.TagIndex;

        _textureSlotHigh = 0;
        Array.Clear(_textureSlots);

        while (_sourceQueue.TryPeek(out _, out var k) && k.TagIndex == tagIndex)
        {
            _sourceQueue.TryDequeue(out var id, out k);
            _textureSlots[k.TextureSlot] = id;
            _textureSlotHigh = int.Max(_textureSlotHigh, k.TextureSlot);
        }
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