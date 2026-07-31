using System.Numerics;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Engine.Render.Passes;
using ConcreteEngine.Engine.Systems;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render;

internal sealed class DrawCommandPipeline : IDisposable
{
    private const int DefaultTicketCapacity = 1024 * 4;

    private NativeArray<int> _drawTickets;
    private readonly Range32[] _passRanges;

    public readonly DrawCommandProcessor DrawCmd;
    private readonly GfxBuffers _gfxBuffers;
    private readonly AnimationSystem _animationSystem;
    private readonly MaterialSystem _materialSystem;

    public DrawCommandPipeline(GfxContext gfx, AnimationSystem animationSystem, MaterialSystem materialSystem) {
        _animationSystem = animationSystem;
        _materialSystem = materialSystem;
        _gfxBuffers = gfx.Buffers;
        DrawCmd = new DrawCommandProcessor(gfx, animationSystem, materialSystem);
        
        _drawTickets = NativeArray.Allocate<int>(DefaultTicketCapacity);
        _passRanges = new Range32[RenderLimits.PassSlots];
    }
    
    public void ResetFrame()
    {
        DrawCmd.ResetFrame();
    }

    public void StageCommands(RenderEcsSystem ecsSystem)
    {
        // Sort command buffer and prepare passes
        ReadyDrawCommands(ecsSystem.DrawIndices);

        // Ensure ubo size
        var drawCount = IntMath.AlignUp(ecsSystem.VisibleCount, 64);
        var materialCount = IntMath.AlignUp(_materialSystem.Count, 16);
        var boneCount = IntMath.AlignUp(_animationSystem.BoneCount, 64);

        if (!GfxRegistry.GetMeta(DrawObjectUniform.UboId).HasCapacity(drawCount))
            _gfxBuffers.SetUniformBufferCount(DrawObjectUniform.UboId, drawCount);

        if (!GfxRegistry.GetMeta(MaterialUniform.UboId).HasCapacity(materialCount))
            _gfxBuffers.SetUniformBufferCount(MaterialUniform.UboId, materialCount);
        
        if (!GfxRegistry.GetMeta(DrawAnimationUniform.UboId).HasCapacity(boneCount))
            _gfxBuffers.SetUniformBufferCount(DrawAnimationUniform.UboId, boneCount);
        
        // Upload
        VisualSystem.Instance.Upload();

        var transforms = ecsSystem.Transforms;
        if (transforms.Length > 0) _gfxBuffers.UploadUniform(transforms, 0);

        var materials = _materialSystem.GetBufferView();
        if (materials.Length > 0) _gfxBuffers.UploadUniform(materials, 0);

        var boneData = _animationSystem.GetBufferView();
        if (boneData.Length > 0) _gfxBuffers.UploadUniform(boneData, 0);

    }

    private AvgFrameTimer avg;

    public unsafe void ExecuteDrawPass(PassId passId, NativeView<RenderEntityId> entities)
    {
        DrawCmd.PrepareDrawPass();
        
        avg.BeginSample();
        
        var passRange = _passRanges[passId];
        var ticket = _drawTickets + passRange.Offset;
        var end = ticket + passRange.Length;
        while (ticket < end)
        {
            var index = *ticket;
            var entity = entities[index];
            DrawCmd.DrawSource(RenderEcs.Core.GetSource(entity), entity, index);
            ++ticket;
        }

        if(avg.EndSample() > 144 * 4) avg.ResetAndPrint();

    }
    
    private unsafe void ReadyDrawCommands(NativeView<DrawCommandIndex> indices)
    {
        if (indices.Length <= 1) return;

        Array.Clear(_passRanges);

        new Span<ulong>((ulong*)indices.Ptr, indices.Length).Sort();
        //indices.AsSpan().Sort();

        var heads = stackalloc int[RenderLimits.PassSlots * 2];

        // Count pass tickets
        CountTickets(indices, heads);

        // Count pass ranges
        var total = CountPasses(heads);

        // Create draw tickets
        if (_drawTickets.Length < total)
        {
            var newSize = CapacityUtils.CapacityGrowthToFit(_drawTickets.Length, total);
            _drawTickets.Resize(newSize, true);
        }

        // fill tickets in sorted order
        FillTickets(indices, heads + RenderLimits.PassSlots);
    }

    private unsafe void CountTickets(NativeView<DrawCommandIndex> indices, int* heads)
    {
        var drawIndex = indices.Ptr;
        var drawIndexEnd = drawIndex + indices.Length;

        while (drawIndex < drawIndexEnd)
        {
            var mask = (uint)drawIndex->Pass;
            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                ++heads[p];
                mask &= mask - 1;
            }

            ++drawIndex;
        }
    }

    private unsafe int CountPasses(int* heads)
    {
        var total = 0;
        for (var p = 0; p < RenderLimits.PassSlots; ++p)
        {
            var c = heads[p];
            heads[RenderLimits.PassSlots + p] += total;
            _passRanges[p] = new Range32(total, c);
            total += c;
        }


        return total;
    }

    private unsafe void FillTickets(NativeView<DrawCommandIndex> indices, int* heads)
    {
        // fill tickets in sorted order
        var drawTickets = _drawTickets;
        
        var drawIndex = indices.Ptr;
        var drawIndexEnd = drawIndex + indices.Length;
        while (drawIndex < drawIndexEnd)
        {
            var idx = drawIndex->Index;
            var mask = (uint)drawIndex->Pass;
            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                var w = heads[p]++;
                drawTickets[w] = idx;
                mask &= mask - 1;
            }

            ++drawIndex;
        }
    }


    public void Dispose() => _drawTickets.Dispose();
}