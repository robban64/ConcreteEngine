using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Render.Components;

namespace ConcreteEngine.Core.Engine.Render;

public enum BitOp : byte
{
    And, AndNot, Or
}

public sealed partial class RenderWorld
{

    public static partial class Query
    {
        [SkipLocalsInit]
        public readonly ref struct QueryFilterData
        {
            public readonly BitOp Op;
            public readonly BitSet Filter1;
            public readonly BitSet Filter2;
            public readonly BitSet Filter3;

            public QueryFilterData(BitOp op, BitSet filter1, BitSet filter2 = default, BitSet filter3 = default)
            {
                Op = op;
                Filter1 = filter1;
                Filter2 = filter2;
                Filter3 = filter3;
            }
            
            public Bit256 Apply(int index)
            {
                var b = ApplyFilter(Op, Filter1.GetBit256(index), Filter2.GetBit256(index));
                if (Filter3.IsNull) return b;
                return ApplyFilter(Op, b, Filter3.GetBit256(index));
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Bit256 ApplyFilter(BitOp op, Bit256 left, Bit256 right)
        {
            return op switch
            {
                BitOp.And => left.And(right),
                BitOp.AndNot => left.AndNot(right),
                BitOp.Or => left.Or(right),
                _ => Bit256.Zero
            };
        }


    }
}