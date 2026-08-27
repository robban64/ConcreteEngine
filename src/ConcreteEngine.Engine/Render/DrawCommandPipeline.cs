using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Engine.Render.Passes;
using ConcreteEngine.Engine.Systems;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Render;

internal sealed class DrawCommandPipeline : IDisposable
{
    private const int DefaultTicketCapacity = 1024 * 4;

    private readonly DrawCommandProcessor _drawCmd;
    private readonly RenderPassContext _passContext;
    private readonly RenderResolver _resolver;

    private readonly Range32[] _passRanges;

    private NativeArray<int> _drawTickets;

    public DrawCommandPipeline(GfxContext gfx, AnimationSystem animationSystem, MaterialSystem materialSystem, RenderResolver resolver)
    {
        _resolver = resolver;
        _drawCmd = new DrawCommandProcessor(gfx, animationSystem, materialSystem);
        _passContext = new RenderPassContext(_drawCmd);
        _drawTickets = NativeArray.Allocate<int>(DefaultTicketCapacity);
        _passRanges = new Range32[RenderLimits.DrawPassSlots];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetFrame()
    {
        _drawCmd.ResetFrame();
        _passContext.ResetFrame();
    }

    public void RunPass(PassId passId)
    {
        var passResult = BeginPass(passId);

        if (passResult.Op is PassOp.Draw)
        {
            _drawCmd.PrepareDrawPass();
            ExecuteDrawPass(_passRanges[passId]);
        }

        EndPass(passId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PassAction BeginPass(PassId passId)
    {
        var passEntry = RenderRegistry.GetPassEntry(passId);
        _passContext.AttachPass(passEntry);
        return passEntry.BeginPassDel(_passContext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EndPass(PassId passId)
    {
        var passEntry = RenderRegistry.GetPassEntry(passId);
        passEntry.EndPassDel?.Invoke(_passContext);
    }

    private void ExecuteDrawPass(Range32 passRange)
    {
        foreach (ref readonly var ticket in _drawTickets.Slice(passRange))
        {
            var entity = _resolver.GetEntity(ticket);
            var source = RenderEcs.Core.GetSource(entity);
            _drawCmd.DrawSource(source, entity, ticket);
        }
    }

    public unsafe void ReadyDrawCommands()
    {
        if (_resolver.VisibleCount <= 1) return;

        Array.Clear(_passRanges);

        var heads = stackalloc int[RenderLimits.DrawPassSlots * 2];

        // Count pass tickets
        CountTickets(_resolver.SortIndices, heads);

        // Count pass ranges
        var total = CountPasses(heads);

        // Create draw tickets
        if (_drawTickets.Length < total)
        {
            var newSize = CapacityUtils.CapacityGrowthToFit(_drawTickets.Length, total);
            _drawTickets.ReAlloc(newSize, true);
        }
        // fill tickets in sorted order
        FillTickets(_resolver.SortIndices, heads + RenderLimits.DrawPassSlots);
    }

    private static unsafe void CountTickets(NativeView<uint> indices, int* heads)
    {
        var drawIndex = indices.Ptr;
        var drawIndexEnd = indices.EndPtr;
        while (drawIndex < drawIndexEnd)
        {
            var mask = (uint)(byte)*drawIndex;
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
        for (var p = 0; p < RenderLimits.DrawPassSlots; ++p)
        {
            var c = heads[p];
            heads[RenderLimits.DrawPassSlots + p] += total;
            _passRanges[p] = new Range32(total, c);
            total += c;
        }

        return total;
    }

    private unsafe void FillTickets(NativeView<uint> indices, int* heads)
    {
        var drawTickets = _drawTickets.Ptr;

        var drawIndex = indices.Ptr;
        var drawIndexEnd = indices.EndPtr;
        while (drawIndex < drawIndexEnd)
        {
            var mask = (uint)(byte)*drawIndex;
            var submitIndex = (int)(drawIndex - indices);
            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                var w = heads[p]++;
                drawTickets[w] = submitIndex;
                mask &= mask - 1;
            }

            ++drawIndex;
        }
    }


    public void Dispose() => _drawTickets.Dispose();
}