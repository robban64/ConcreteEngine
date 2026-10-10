using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Render.Components;

namespace ConcreteEngine.Core.Engine.Render;

public sealed partial class RenderWorld
{
    public static partial class Query
    {
        public readonly ref struct ChunkQueryItem<T1>(int blockIdx, Bit256 chunk)
            where T1 : unmanaged, IRenderComponent<T1>

        {
            public readonly int BlockIdx = blockIdx;
            public readonly Span<T1> Data1 = Dense<T1>().AsSpan();
            public readonly Bit256 Chunk = chunk;

            public int Lanes => int.Min(EntityBlockCount - BlockIdx, 4);

            public FilterEnumerator<T1> EnumerateLane(int lane)
            {
                return new FilterEnumerator<T1>(Chunk.GetBit64(lane), (BlockIdx + lane) << 6, Data1);
            }

            public delegate void QueryAction<TContext>(ref TContext ctx, int lane, int start, Bit64 block, Span<T1> data1)
                where TContext : struct, allows ref struct;

            public void ForEachLane<TContext>(ref TContext ctx, QueryAction<TContext> action)
                where TContext : struct, allows ref struct
            {
                var lanes = Lanes;
                for (int lane = 0; lane < lanes; lane++)
                {
                    var chunk = Chunk.GetBit64(lane);
                    if (chunk.IsSet)
                    {
                        var blockIndex = (BlockIdx + lane) << 6;
                        action(ref ctx, lane, blockIndex, chunk, Data1);
                    }
                }
            }
        }
        
        public readonly ref struct ChunkQueryItem<T1, T2>(int blockIdx, Bit256 chunk)
            where T1 : unmanaged, IRenderComponent<T1> 
            where T2 : unmanaged, IRenderComponent<T2>

        {
            public readonly int BlockIdx = blockIdx;
            public readonly Span<T1> Data1 = Dense<T1>().AsSpan();
            public readonly Span<T2> Data2 = Dense<T2>().AsSpan();

            public readonly Bit256 Chunk = chunk;

            public int Lanes => int.Min(EntityBlockCount - BlockIdx, 4);

            public FilterEnumerator<T1, T2> EnumerateLane(int lane)
            {
                return new FilterEnumerator<T1, T2>(Chunk.GetBit64(lane), (BlockIdx + lane) << 6, Data1, Data2);
            }

            public delegate void QueryAction<TContext>(ref TContext ctx, int lane, int start, Bit64 block, Span<T1> data1, Span<T2> data2)
                where TContext : struct, allows ref struct;

            public void ForEachLane<TContext>(ref TContext ctx, QueryAction<TContext> action)
                where TContext : struct, allows ref struct
            {
                var lanes = Lanes;
                for (int lane = 0; lane < lanes; lane++)
                {
                    var block = Chunk.GetBit64(lane);
                    if (block.IsSet)
                    {
                        var start = (BlockIdx + lane) << 6;
                        action(ref ctx, lane, start, block, Data1, Data2);
                    }
                }
            }
        }
    }
}
