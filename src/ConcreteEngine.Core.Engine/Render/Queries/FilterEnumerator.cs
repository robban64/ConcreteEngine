using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Render.Components;

namespace ConcreteEngine.Core.Engine.Render;

public sealed partial class RenderWorld
{
    public static partial class Query
    {
        public ref struct FilterEnumerator<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            private readonly int _start;
            private int _bit;
            private int _entity;

            private Bit64 _currentBits;
            private readonly Span<T1> _data1;

            public FilterEnumerator(Bit64 entityBits, int start, Span<T1> data1)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(start);
                _bit = 0;
                _start = start;
                _entity = 0;
                _currentBits = entityBits;
                _data1 = data1.Slice(start);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (_currentBits.IsEmpty) return false;
                _bit = BitOperations.TrailingZeroCount(_currentBits);
                _entity = _bit + _start;
                _currentBits.ClearLowerBits();
                return true;
            }

            public readonly QueryItem<T1> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(_entity, _bit, ref _data1[_bit]);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterEnumerator<T1> GetEnumerator() => this;
        }
        

        public ref struct FilterEnumerator<T1, T2> 
            where T1 : unmanaged, IRenderComponent<T1>
            where T2 : unmanaged, IRenderComponent<T2>
        {
            private readonly int _start;
            private int _bit;
            private int _entity;

            private Bit64 _currentBits;
            private readonly Span<T1> _data1;
            private readonly Span<T2> _data2;

            public FilterEnumerator(Bit64 entityBits, int start, Span<T1> data1, Span<T2> data2)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(start);
                _bit = 0;
                _start = start;
                _entity = 0;
                _currentBits = entityBits;
                _data1 = data1.Slice(start);
                _data2 = data2.Slice(start);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (_currentBits.IsEmpty) return false;
                _bit = BitOperations.TrailingZeroCount(_currentBits);
                _entity = _bit + _start;
                _currentBits.ClearLowerBits();
                return true;
            }

            public readonly QueryItem<T1, T2> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(_entity, _bit, ref _data1[_bit], ref _data2[_bit]);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterEnumerator<T1, T2> GetEnumerator() => this;
        }
    }
}