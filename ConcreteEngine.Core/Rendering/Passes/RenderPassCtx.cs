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
    private PassCommandQueue _cmdQueue;
    private IReadOnlyDictionary<PassTagKey, RenderPassEntry> _registry;
    private Size2D _outputSize;

    public PipelineStateOps CmdOps { get; private set; }
    public RenderTargetInfo Target { get; private set; }
    public int Pass { get; private set; }
    public PassTagKey TagKey { get; private set; }


    internal RenderPassCtx(
        PipelineStateOps cmdOps, 
        PassCommandQueue cmdQueue,
        IReadOnlyDictionary<PassTagKey, RenderPassEntry> registry, Size2D outputSize)
    {
        CmdOps = cmdOps;
        _registry = registry;
        _outputSize = outputSize;
        _cmdQueue = cmdQueue;
        
    }

    internal void AttachScreenPass(int pass, PassTagKey tagKey)
    {
        Target = new RenderTargetInfo(default, _outputSize, default, default);
        Pass = pass;
        TagKey = tagKey;
    }

    internal void AttachPass(RenderFbo fbo, int pass, PassTagKey tagKey)
    {
        Target = new RenderTargetInfo(fbo.FboId, fbo.Size, fbo.Attachments, fbo.MultiSample);
        Pass = pass;
        TagKey = tagKey;
    }

    public IReadOnlyList<TextureId> GetPassSources()
    {
        return _cmdQueue.DequeuePassSources(TagKey);
    }

    public void SampleTo<TTag, TSlot>(PassOpKind passOp, int slot, TextureId textureId)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot
    {
        Debug.Assert(slot >= 0 && slot < 16);

        var key = PassTextureSlotKey.Make<TTag, TSlot>(passOp, (byte)slot);
        _cmdQueue.SampleTo(key, textureId);
    }

    public void MutateStatePass<TTag, TSlot>(PassOpKind passOp, in PassMutationState newState)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot
    {
        var key = PassTagKey.Make<TTag, TSlot>(passOp);
        if (_registry.TryGetValue(key, out RenderPassEntry entry))
        {
            entry.UpdateState(in newState);
        }
    }

    public void MutateStatePass<TTag, TSlot>(PassOpKind passOp, RenderPassMutate mutate)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot
    {
        var key = PassTagKey.Make<TTag, TSlot>(passOp);
        if (_registry.TryGetValue(key, out RenderPassEntry entry))
        {
            entry.UpdateState(mutate);
        }
    }
}