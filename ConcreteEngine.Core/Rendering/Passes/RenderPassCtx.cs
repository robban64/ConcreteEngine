#region

using System.Diagnostics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Gfx;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Passes;

public sealed class RenderPassCtx
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

    internal void AttachScreenPass( PassTagKey tagKey, Size2D outputSize)
    {
        Target = new RenderTargetInfo(default, outputSize, default, default);
        CurrentPassKey = tagKey;
    }

    internal void AttachPass(RenderFbo fbo, PassTagKey tagKey)
    {
        Target = new RenderTargetInfo(fbo.FboId, fbo.Size, fbo.Attachments, fbo.MultiSample);
        CurrentPassKey = tagKey;
    }

    public IReadOnlyList<TextureId> GetPassSources() => _cmdQueue.GetPassSources();

    public void SampleTo<TTag>(FboVariant variant, int texSlot, TextureId textureId)
        where TTag : unmanaged, IRenderPassTag 
    {
        Debug.Assert(texSlot >= 0 && texSlot < 16);

        var passKey = TagRegistry.PassKey<TTag>(variant);
        var key = new PassTextureSlotKey(passKey.TagIndex, passKey.Variant, passKey.Pass, (byte)texSlot);
        _cmdQueue.SampleTo(key, textureId);
    }

    public void MutateStatePass<TTag>(FboVariant variant, in PassMutationState newState)
        where TTag : unmanaged, IRenderPassTag 
    {
        var key = TagRegistry.PassKey<TTag>(variant);
        _cmdQueue.EnqueueMutation(key, in newState);
    }

}