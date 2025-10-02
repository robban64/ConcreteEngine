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

    public PipelineStateOps Ops { get; private set; }
    public RenderTargetInfo Target { get; private set; }
    public int Pass { get; private set; }
    public PassTagKey TagKey { get; private set; }


    internal RenderPassCtx(PipelineStateOps cmdOps, PassCommandQueue cmdQueue)
    {
        Ops = cmdOps;
        _cmdQueue = cmdQueue;
    }

    internal void AttachScreenPass(int pass, PassTagKey tagKey, Size2D outputSize)
    {
        Target = new RenderTargetInfo(default, outputSize, default, default);
        Pass = pass;
        TagKey = tagKey;
    }

    internal void AttachPass(RenderFbo fbo, int pass, PassTagKey tagKey)
    {
        Target = new RenderTargetInfo(fbo.FboId, fbo.Size, fbo.Attachments, fbo.MultiSample);
        Pass = pass;
        TagKey = tagKey;
    }
    

    public IReadOnlyList<TextureId> GetPassSources() => _cmdQueue.GetPassSources();

    public void SampleTo<TTag, TSlot>(PassOpKind passOp, int texSlot, TextureId textureId)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot
    {
        Debug.Assert(texSlot >= 0 && texSlot < 16);

        var key = PassTextureSlotKey.Make<TTag, TSlot>(passOp, (byte)texSlot);
        _cmdQueue.SampleTo(key, textureId);
    }

    public void MutateStatePass<TTag, TSlot>(PassOpKind passOp, in PassMutationState newState)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot
    {
        var key = PassTagKey.Make<TTag, TSlot>(passOp);
        _cmdQueue.EnqueueMutation(key, in newState);
    }
/*
    public void MutateStatePass<TTag, TSlot>(PassOpKind passOp, RenderPassMutate mutate)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot
    {
        var key = PassTagKey.Make<TTag, TSlot>(passOp);
        if (_registry.TryGetValue(key, out RenderPassEntry entry))
        {
            entry.UpdateState(mutate);
        }
    }
    */
}