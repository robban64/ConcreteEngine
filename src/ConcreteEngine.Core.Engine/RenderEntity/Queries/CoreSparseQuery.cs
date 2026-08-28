using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Engine.RenderEntity.Queries;

public static unsafe partial class RenderCoreQuery 
{
    public ref struct SparseQueryEnumerator<T> where T : unmanaged
    {
        private readonly T* _p1;
        private RenderEntityId* _current;
        private readonly RenderEntityId* _end;

        public SparseQueryEnumerator(NativeView<RenderEntityId> entities, NativeView<T> p1)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(entities.Length, p1.Length);
            _p1 = p1;
            _current = entities - 1;
            _end = entities.EndPtr;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_current < _end;

        public readonly QueryItem<T> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(*_current, ref _p1[_current->Index()]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly SparseQueryEnumerator<T> GetEnumerator() => this;
    }

    public ref struct SparseQueryEnumerator<T1, T2> where T1 : unmanaged where T2 : unmanaged
    {
        private readonly T1* _p1;
        private readonly T2* _p2;

        private RenderEntityId* _current;
        private readonly RenderEntityId* _end;

        public SparseQueryEnumerator(NativeView<RenderEntityId> entities, NativeView<T1> p1, NativeView<T2> p2)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(entities.Length, p1.Length);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(entities.Length, p2.Length);

            _p1 = p1;
            _p2 = p2;
            _current = entities - 1;
            _end = entities.EndPtr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_current < _end;

        public readonly QueryItem<T1, T2> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(*_current, ref _p1[_current->Index()], ref _p2[_current->Index()]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly SparseQueryEnumerator<T1, T2> GetEnumerator() => this;
    }
}