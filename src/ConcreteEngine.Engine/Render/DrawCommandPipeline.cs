using System.Numerics;
using System.Runtime.CompilerServices;
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

    public readonly DrawCommandProcessor DrawCmd;

    private NativeArray<(RenderEntityId, int)> _drawTickets;
    private readonly Range32[] _passRanges;

    private readonly GfxBuffers _gfxBuffers;
    private readonly AnimationSystem _animationSystem;
    private readonly MaterialSystem _materialSystem;

    public DrawCommandPipeline(GfxContext gfx, AnimationSystem animationSystem, MaterialSystem materialSystem)
    {
        _animationSystem = animationSystem;
        _materialSystem = materialSystem;
        _gfxBuffers = gfx.Buffers;
        DrawCmd = new DrawCommandProcessor(gfx, animationSystem, materialSystem);

        _drawTickets = NativeArray.Allocate<(RenderEntityId, int)>(DefaultTicketCapacity);
        _passRanges = new Range32[RenderLimits.PassSlots];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetFrame() => DrawCmd.ResetFrame();

    public static AvgFrameTimer avg;
    public void StageCommands(RenderResolver resolver)
    {
        avg.BeginSample();
        ReadyDrawCommands();
        avg.EndSample();
        UploadBuffers(resolver.Transforms);
    }

    public Range32 PrepareDrawPass(PassId passId)
    {
        DrawCmd.PrepareDrawPass();
        return _passRanges[passId];
    }

    public unsafe void ExecuteDrawPass(Range32 passRange)
    {
        var sources = RenderEcs.Core.GetSourceView().Ptr;
        foreach (var ticket in _drawTickets.Slice(passRange))
        {
            var source = sources[ticket.Item1.Index()];
            DrawCmd.DrawSource(source, ticket.Item1, ticket.Item2);
        }
    }

    private void UploadBuffers(NativeView<TransformUniform> transforms)
    {
        // Ensure ubo size
        var drawCount = IntMath.AlignUp(RenderEcs.VisibleCount, 64);
        var materialCount = IntMath.AlignUp(_materialSystem.Count, 16);
        var boneCount = IntMath.AlignUp(_animationSystem.BoneCount, 64);

        if (!GfxRegistry.GetMeta(TransformUniform.UboId).HasCapacity(drawCount))
            _gfxBuffers.SetUniformBufferCount(TransformUniform.UboId, drawCount);

        if (!GfxRegistry.GetMeta(MaterialUniform.UboId).HasCapacity(materialCount))
            _gfxBuffers.SetUniformBufferCount(MaterialUniform.UboId, materialCount);

        if (!GfxRegistry.GetMeta(SkinningUniform.UboId).HasCapacity(boneCount))
            _gfxBuffers.SetUniformBufferCount(SkinningUniform.UboId, boneCount);

        // Upload
        VisualSystem.Instance.Upload();

        if (transforms.Length > 0) _gfxBuffers.UploadUniform(transforms, 0);

        var materials = _materialSystem.GetUniforms();
        if (materials.Length > 0) _gfxBuffers.UploadUniform(materials, 0);

        var boneData = _animationSystem.GetUniforms();
        if (boneData.Length > 0) _gfxBuffers.UploadUniform(boneData, 0);
    }

    private unsafe void ReadyDrawCommands()
    {
        if (RenderEcs.Frame.VisibleCount <= 1) return;

        Array.Clear(_passRanges);

        var heads = stackalloc int[RenderLimits.PassSlots * 2];

        // Count pass tickets
        CountTickets(RenderEcs.Frame.VisibleEntities, heads);

        // Count pass ranges
        var total = CountPasses(heads);

        // Create draw tickets
        if (_drawTickets.Length < total)
        {
            var newSize = CapacityUtils.CapacityGrowthToFit(_drawTickets.Length, total);
            _drawTickets.ReAlloc(newSize, true);
        }

        // fill tickets in sorted order
        FillTickets(RenderEcs.Frame.VisibleEntities, heads + RenderLimits.PassSlots);
    }

    private unsafe void CountTickets(NativeView<DrawEntityIndex> indices, int* heads)
    {
        var drawIndex = indices.Ptr;
        var drawIndexEnd = drawIndex + indices.Length;

        while (drawIndex < drawIndexEnd)
        {
            var mask = (uint)drawIndex->Mask;
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

    private unsafe void FillTickets(NativeView<DrawEntityIndex> indices, int* heads)
    {
        // fill tickets in sorted order
        var drawTickets = _drawTickets;

        var submitIndex = 0;
        var drawIndex = indices.Ptr;
        var drawIndexEnd = drawIndex + indices.Length;
        while (drawIndex < drawIndexEnd)
        {
            var mask = (uint)drawIndex->Mask;
            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                var w = heads[p]++;
                drawTickets[w] = (drawIndex->Entity, submitIndex);
                mask &= mask - 1;
            }
            ++drawIndex;
            ++submitIndex;
        }
    }


    public void Dispose() => _drawTickets.Dispose();
}