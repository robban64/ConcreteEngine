using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer.Passes;

internal sealed class RenderPassCtx
{
    private RenderTargetInfo _target;
    public ref readonly RenderTargetInfo Target => ref _target;
    public PassTagKey CurrentPassKey { get; private set; }

    public readonly PassCommandQueue PassQueue;

    public readonly GfxCommands GfxCmd;

    private readonly GfxTextures _gfxTextures;

    private readonly UniformUploader _uniformUploader;

    internal RenderPassCtx(GfxContext gfx, UniformUploader uniformUploader)
    {
        PassQueue = new PassCommandQueue();
        _uniformUploader = uniformUploader;
        GfxCmd = gfx.Commands;
        _gfxTextures = gfx.Textures;
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
    public ReadOnlySpan<TextureId> GetPassSources() => PassQueue.GetPassSources();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SampleTo<TTarget>(FboVariant variant, TexSlot texSlot) where TTarget : unmanaged, IRenderTarget
    {
        Debug.Assert(texSlot.Slot < RenderLimits.TextureSlots);

        var passKey = TargetRegistry<TTarget>.PassKey(variant);
        var key = new PassTextureSlotKey(passKey.TagIndex, passKey.Variant, passKey.Pass, texSlot.Slot);
        PassQueue.SampleTo(key, texSlot.Texture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MutateStatePass<TTarget>(FboVariant variant, in PassMutationState newState) where TTarget : unmanaged, IRenderTarget
    {
        var key = TargetRegistry<TTarget>.PassKey(variant);
        PassQueue.EnqueueMutation(key, in newState);
    }

    //

    public void ActivateDepthMode()
    {
        RenderContext.Instance.SetDepthMode();
        _uniformUploader.UploadViewUniforms();
    }

    public void RestoreMode()
    {
        RenderContext.Instance.ResetPassMode();
        _uniformUploader.UploadViewUniforms();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ContinueFromRenderPass(FrameBufferId fboId, GfxStateFlags passFlags)
    {
        GfxCmd.BindFramebuffer(fboId);
        GfxCmd.ApplyPassState(passFlags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GenerateMips(TextureId textureId) => _gfxTextures.GenerateMipMaps(textureId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawFullscreenQuad(ShaderId shaderId, ReadOnlySpan<TextureId> sources)
    {
        GfxCmd.UseShader(shaderId);

        for (var i = 0; i < sources.Length; i++)
            GfxCmd.BindTexture(sources[i], i);

        GfxCmd.DrawMesh(GfxMeshes.FsqQuad);
    }

    public void SetOutputTexture(TextureId textureId)
    {
        RenderContext.Instance.OutputTexture = textureId;
    }
}