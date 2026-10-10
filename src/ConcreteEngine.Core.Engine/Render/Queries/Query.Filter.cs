using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Render;

public enum BitOp : byte
{
    And, AndNot, Or
}

public sealed partial class RenderWorld
{
    public static partial class Query
    {
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
                var b = ApplyFilter(Op, Filter1.Get256(index), Filter2.Get256(index));
                if (Filter3.IsNull) return b;
                return ApplyFilter(Op, b, Filter3.Get256(index));
            }
            
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Bit256 ApplyFilter(BitOp op, Bit256 left, Bit256 right)
            {
                return op switch
                {
                    BitOp.And => Bit256.And(left, right),
                    BitOp.AndNot => Bit256.AndNot(left, right),
                    BitOp.Or => Bit256.Or(left, right),
                    _ => Bit256.Zero
                };
            }
        }

    }
}