using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Engine.Render.Registry;
using ConcreteEngine.Engine.Render.Renderer;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render.Passes;

internal sealed class RenderPassCtx
{
    private int _textureSlotHigh;
    private RenderTargetInfo _target;
    public PassTagKey CurrentPassKey { get; private set; }
    
    public readonly GfxCommands Cmd;
    private readonly GfxTextures _gfxTextures;

    private readonly PriorityQueue<TextureId, PassTextureSlotKey> _sourceQueue;
    private readonly PriorityQueue<PassMutationState, PassTagKey> _mutationQueue;
    private readonly TextureId[] _textureSlots;

    internal RenderPassCtx(GfxContext gfx)
    {
        Cmd = gfx.Commands;
        _gfxTextures = gfx.Textures;
        _sourceQueue = new PriorityQueue<TextureId, PassTextureSlotKey>(4, new PassTextureSlotKeyComp());
        _mutationQueue = new PriorityQueue<PassMutationState, PassTagKey>(4, new PassTagKeyComp());
        _textureSlots = new TextureId[RenderLimits.TextureSlots];
    }

    public ref readonly RenderTargetInfo Target => ref _target;

    internal void Prepare()
    {
        _sourceQueue.Clear();
        _mutationQueue.Clear();
        _textureSlots.AsSpan().Clear();
        _textureSlotHigh = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AttachScreenPass(PassTagKey tagKey, Size2D outputSize)
    {
        _target = new RenderTargetInfo(default, outputSize, default, default);
        CurrentPassKey = tagKey;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AttachPass(RenderFbo fbo, PassTagKey tagKey)
    {
        var meta = GfxRegistry.GetMeta(fbo.FboId);
        _target = new RenderTargetInfo(fbo.FboId, meta.Size, meta.Attachments, meta.MultiSample);
        CurrentPassKey = tagKey;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            entry.UpdateState(in state);
        }
    }

    public void DequeuePassSources(RenderPassEntry entry)
    {
        var tagIndex = entry.PassKey.TagIndex;
        var slots = _textureSlots.AsSpan();
        slots.Clear();

        _textureSlotHigh = 0;
        while (_sourceQueue.TryPeek(out _, out var k) && k.TagIndex == tagIndex)
        {
            _sourceQueue.TryDequeue(out var id, out k);
            slots[k.TextureSlot] = id;
            _textureSlotHigh = int.Max(_textureSlotHigh, k.TextureSlot);
        }
    }

    //

    public void ActivateDepthMode()
    {
        RenderContext.Instance.SetDepthMode();
        VisualUniformProcessor.Instance.UploadShadow();
        VisualUniformProcessor.Instance.UploadLightView();
    }

    public void RestoreMode()
    {
        RenderContext.Instance.ResetPassMode();
        VisualUniformProcessor.Instance.UploadMainView();
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