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
    private readonly GfxDraw _gfxDraw;

    private readonly UniformUploader _uniformUploader;

    internal RenderPassCtx( GfxContext gfx, UniformUploader uniformUploader)
    {
        PassQueue = new PassCommandQueue();
        _uniformUploader = uniformUploader;
        GfxCmd = gfx.Commands;
        _gfxTextures = gfx.Textures;
        _gfxDraw = gfx.Draw;

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
        _target = new RenderTargetInfo(fbo.FboId, fbo.Size, fbo.Attachments, fbo.MultiSample);
        CurrentPassKey = tagKey;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<TextureId> GetPassSources() => PassQueue.GetPassSources();


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SampleTo<TTag>(FboVariant variant, TexSlot texSlot) where TTag : class
    {
        Debug.Assert(texSlot.Slot >= 0 && texSlot.Slot < RenderLimits.TextureSlots);

        var passKey = PassTags<TTag>.PassKey(variant);
        var key = new PassTextureSlotKey(passKey.TagIndex, passKey.Variant, passKey.Pass, (byte)texSlot.Slot);
        PassQueue.SampleTo(key, texSlot.Texture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MutateStatePass<TTag>(FboVariant variant, in PassMutationState newState) where TTag : class
    {
        var key = PassTags<TTag>.PassKey(variant);
        PassQueue.EnqueueMutation(key, in newState);
    }

    //

    public void ActivateDepthMode()
    {
        VisualRenderContext.Instance.SetDepthMode();
        _uniformUploader.UploadViewUniforms();
    }

    public void RestoreMode()
    {
        VisualRenderContext.Instance.ResetPassMode();
        _uniformUploader.UploadViewUniforms();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ContinueFromRenderPass(FrameBufferId fboId, GfxPassState states)
    {
        GfxCmd.BindFramebuffer(fboId);
        GfxCmd.ApplyPassState(states);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GenerateMips(TextureId textureId) => _gfxTextures.GenerateMipMaps(textureId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawFullscreenQuad(ShaderId shaderId, ReadOnlySpan<TextureId> sources)
    {
        GfxCmd.UseShader(shaderId);

        for (var i = 0; i < sources.Length; i++)
            GfxCmd.BindTexture(sources[i], i);

        _gfxDraw.BindDraw(GfxMeshes.FsqQuad);
    }

    public void SetOutputTexture(TextureId textureId)
    {
        VisualRenderContext.Instance.OutputTexture = textureId;
    }
}