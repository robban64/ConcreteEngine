using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer.Passes;

internal sealed class RenderPassCtx
{
    private readonly PassCommandQueue _cmdQueue;

    public DrawStateOps Ops { get; private set; }
    public RenderTargetInfo Target { get; private set; }
    public PassTagKey CurrentPassKey { get; private set; }


    internal RenderPassCtx(DrawStateOps cmdOps, PassCommandQueue cmdQueue)
    {
        Ops = cmdOps;
        _cmdQueue = cmdQueue;
    }

    internal void AttachScreenPass(PassTagKey tagKey, Size2D outputSize)
    {
        Target = new RenderTargetInfo(default, outputSize, default, default);
        CurrentPassKey = tagKey;
    }

    [SkipLocalsInit]
    internal void AttachPass(RenderFbo fbo, PassTagKey tagKey)
    {
        Target = new RenderTargetInfo(fbo.FboId, fbo.Size, fbo.Attachments, fbo.MultiSample);
        CurrentPassKey = tagKey;
    }

    public ReadOnlySpan<TextureId> GetPassSources() => _cmdQueue.GetPassSources();


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SampleTo<TTag>(FboVariant variant, TexSlot texSlot) where TTag : class
    {
        Debug.Assert(texSlot.Slot >= 0 && texSlot.Slot < RenderLimits.TextureSlots);

        var passKey = PassTags<TTag>.PassKey(variant);
        var key = new PassTextureSlotKey(passKey.TagIndex, passKey.Variant, passKey.Pass, (byte)texSlot.Slot);
        _cmdQueue.SampleTo(key, texSlot.Texture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MutateStatePass<TTag>(FboVariant variant, in PassMutationState newState) where TTag : class
    {
        var key = PassTags<TTag>.PassKey(variant);
        _cmdQueue.EnqueueMutation(key, in newState);
    }
}