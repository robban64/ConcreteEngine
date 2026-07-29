using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Systems;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render.Passes;

internal sealed class RenderPassContext
{
    private int _textureSlotHigh;
    
    public FrameBufferId FboId { get; private set; }
    public PassTargetKey CurrentPassKey { get; private set; }
    
    public readonly GfxCommands Cmd;
    public readonly GfxBuffers Buffers;
    private readonly GfxTextures _gfxTextures;
    public readonly DrawCommandProcessor DrawCmd;

    private readonly PriorityQueue<TextureId, PassTextureSlotKey> _sourceQueue;
    private readonly PriorityQueue<PassMutationState, PassTargetKey> _mutationQueue;
    private readonly TextureId[] _textureSlots;

    internal RenderPassContext(GfxContext gfx, DrawCommandProcessor drawCmd)
    {
        DrawCmd = drawCmd;
        Cmd = gfx.Commands;
        Buffers = gfx.Buffers;
        _gfxTextures = gfx.Textures;
        _sourceQueue = new PriorityQueue<TextureId, PassTextureSlotKey>(4, new PassTextureSlotKeyComp());
        _mutationQueue = new PriorityQueue<PassMutationState, PassTargetKey>(4, new PassTagKeyComp());
        _textureSlots = new TextureId[RenderLimits.TextureSlots];
    }

    public ref readonly FrameBufferMeta Target => ref GfxRegistry.GetMeta(FboId);

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
    public void SampleTo<TTarget>(FboVariant variant, TexSlot texSlot) where TTarget : unmanaged, IRenderTarget
    {
        Debug.Assert(texSlot.Slot < RenderLimits.TextureSlots);
        var passKey = RenderRegistry.TargetRegistry<TTarget>.PassKey(variant);
        var key = new PassTextureSlotKey(passKey.TagIndex, passKey.Variant, passKey.Pass, texSlot.Slot);
        _sourceQueue.Enqueue(texSlot.Texture, key);
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
        RenderContext.SetDepthMode();
        VisualSystem.Instance.UploadShadow();
        VisualSystem.Instance.UploadLightView();
    }

    public void RestoreMode()
    {
        RenderContext.ResetPassMode();
        VisualSystem.Instance.UploadMainView();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ContinueFromRenderPass(FrameBufferId fboId, GfxStateFlags passFlags)
    {
        Cmd.BindFramebuffer(fboId);
        Cmd.ApplyPassState(passFlags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GenerateMips(TextureId textureId) => _gfxTextures.GenerateMipMaps(textureId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawFullscreenQuad(ShaderId shaderId, ReadOnlySpan<TextureId> sources)
    {
        Cmd.UseShader(shaderId);
        
        for (var i = 0; i < sources.Length; i++) 
            Cmd.BindTexture(sources[i], i);

        Cmd.DrawMesh(GfxMeshes.FsqQuad);
    }

}