using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Engine.Render.Components;
using static ConcreteEngine.Core.Engine.Render.RenderWorld.Query;

namespace ConcreteEngine.Core.Engine.Render;

public sealed partial class RenderWorld
{
    public static partial class Query
    {
        public ref struct FilterQuery<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            private int _blockIndex;
            private readonly int _blockLength;

            private readonly BitSet64 _filter1;
            private readonly BitSet64 _filter2;

            private readonly Span<T1> _data;
            
            public FilterQuery(BitSet64 filter1, BitSet64 filter2)
            {
                _blockIndex = -1;
                _blockLength = filter1.BlockCount;
                _filter1 = filter1;
                _filter2 = filter2.IsNull ? Meta.TrueSet : filter2;
                _data = Dense<T1>().AsSpan();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_blockIndex < _blockLength)
                {
                    var block = BitSet64.And(_blockIndex, _filter1, _filter2);
                    if (block.IsSet)
                    {
                        var start = _blockIndex * 64;
                        var length = int.Min(start + 64, _data.Length) - start;
                        Current = new FilterEnumerator<T1>(block, start, _data.Slice(start, length));
                        return true;
                    }
                }

                return false;

            }
            
            public FilterEnumerator<T1> Current { get; private set; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterQuery<T1> GetEnumerator() => this;
        }
        
        public ref struct FilterEnumerator<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            private Bit64 _bits;
            private readonly Span<T1> _data;

            private int _bit;
            private readonly int _start;

            public FilterEnumerator(Bit64 bits, int start, Span<T1> data)
            {
                _bit = 0;
                _bits = bits;
                _start = start;
                _data = data;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_bits.IsSet)
                {
                    var bit = BitOperations.TrailingZeroCount(_bits);
                    _bits.ClearLowerBits();
                    if (bit < _data.Length)
                    {
                        _bit = bit;
                        return true;
                    }
                }

                return false;
            }


            public readonly QueryItem<T1> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(_start + _bit, ref _data[_bit]);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterEnumerator<T1> GetEnumerator() => this;
        }
        

        public ref struct FilterQuery<T1, T2> 
            where T1 : unmanaged, IRenderComponent<T1>
            where T2 : unmanaged, IRenderComponent<T2>
        {
            private int _blockIndex;
            private readonly int _blockLength;

            private readonly BitSet64 _filter1;
            private readonly BitSet64 _filter2;
            
            private readonly Span<T1> _data1;
            private readonly Span<T2> _data2;

            public FilterQuery(BitSet64 filter1, BitSet64 filter2)
            {
                _filter1 = filter1;
                _filter2 = filter2.IsNull ? Meta.TrueSet : filter2;
                
                _blockIndex = -1;
                _blockLength = filter1.BlockCount;
                _filter1 = filter1;
                _filter2 = filter2.IsNull ? Meta.TrueSet : filter2;

                _data1 = Dense<T1>().AsSpan();
                _data2 = Dense<T2>().AsSpan();
                if(_data1.Length != _data2.Length) Throwers.InvalidOperation();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_blockIndex < _blockLength)
                {
                    var block = BitSet64.And(_blockIndex, _filter1, _filter2);
                    if (block.IsSet)
                    {
                        var start = _blockIndex * 64;
                        var length = int.Min(start + 64, _data1.Length) - start;
                        Current = new FilterEnumerator<T1,T2>(block, start, _data1.Slice(start, length),_data2.Slice(start, length));
                        return true;
                    }
                }

                return false;

            }

            public FilterEnumerator<T1, T2> Current { get; private set; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterQuery<T1,T2> GetEnumerator() => this;
        }
    }
    
    public ref struct FilterEnumerator<T1,T2>            
        where T1 : unmanaged, IRenderComponent<T1>
        where T2 : unmanaged, IRenderComponent<T2>

    {
        private int _bit;
        private readonly int _start;

        private Bit64 _bits;
        private readonly Span<T1> _data1;
        private readonly Span<T2> _data2;

        public FilterEnumerator(Bit64 bits, int start, Span<T1> data1, Span<T2> data2)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(data1.Length, data2.Length);
            _bit = 0;
            _bits = bits;
            _start = start;
            _data1 = data1;
            _data2 = data2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (_bits.IsSet)
            {
                var bit = BitOperations.TrailingZeroCount(_bits);
                _bits.ClearLowerBits();
                if (bit < _data1.Length)
                {
                    _bit = bit;
                    return true;
                }
            }

            return false;
        }


        public readonly QueryItem<T1,T2> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_start + _bit, ref _data1[_bit], ref _data2[_bit]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly FilterEnumerator<T1,T2> GetEnumerator() => this;
    }

}