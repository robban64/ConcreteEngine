using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Core.Engine.RenderEntity;

public sealed unsafe partial class RenderEntityCore
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public QueryEnumerator<BoundingAxisBox> CullQuery(EntityDrawStatus skipFlag = EntityDrawStatus.ForceHidden) =>
        new(_policies, _bounds, Count, skipFlag);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SparseQueryEnumerator<DrawPolicy, BoundingAxisBox> DrawPolicyQuery(NativeView<DrawEntityCommand> entities) =>
        new(entities, _policies, _bounds, entities.Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SparseQueryEnumerator<Matrix4x4, Matrix3X4> TransformQuery(NativeView<DrawEntityCommand> entities) =>
        new(entities, _models, _normals, entities.Length);

    //
    public readonly ref struct QueryItem<T1>(RenderEntityId entity, ref T1 item1) where T1 : unmanaged
    {
        public readonly RenderEntityId Entity = entity;
        public readonly ref T1 Item1 = ref item1;
    }
    public readonly ref struct QueryItem<T1, T2>(RenderEntityId entity, ref T1 item1, ref T2 item2)
        where T1 : unmanaged where T2 : unmanaged
    {
        public readonly RenderEntityId Entity = entity;
        public readonly ref T1 Item1 = ref item1;
        public readonly ref T2 Item2 = ref item2;
    }

    public readonly ref struct VisibleQueryItem<T1>(ref DrawEntityCommand entityCmd, ref T1 item1) where T1 : unmanaged
    {
        public readonly ref DrawEntityCommand EntityCmd = ref entityCmd;
        public readonly ref T1 Item1 = ref item1;
    }

    public readonly ref struct VisibleQueryItem<T1, T2>(ref DrawEntityCommand entityCmd, ref T1 item1, ref T2 item2)
        where T1 : unmanaged where T2 : unmanaged
    {
        public readonly ref DrawEntityCommand EntityCmd = ref entityCmd;
        public readonly ref T1 Item1 = ref item1;
        public readonly ref T2 Item2 = ref item2;
    }

    public ref struct QueryEnumerator<T> where T : unmanaged
    {
        private T* _p1;
        private DrawPolicy* _current;
        private readonly DrawPolicy* _end;
        private int _entity;
        private readonly EntityDrawStatus _skipStatus;

        public QueryEnumerator(DrawPolicy* current, T* p1, int length, EntityDrawStatus skipStatus)
        {
            _skipStatus = skipStatus;
            _entity = 0;
            _current = current - 1;
            _p1 = p1 - 1;
            _end = current + length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_current < _end)
            {
                ++_p1;
                ++_entity;
                if (_current->Status != 0 && _current->Status != _skipStatus) return true;
            }

            return false;
        }

        public readonly QueryItem<DrawPolicy, T> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(new RenderEntityId(_entity), ref *_current, ref *_p1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly QueryEnumerator<T> GetEnumerator() => this;
    }

    public ref struct SparseQueryEnumerator<T> where T : unmanaged
    {
        private readonly T* _p1;
        private DrawEntityCommand* _current;
        private readonly DrawEntityCommand* _end;

        public SparseQueryEnumerator(DrawEntityCommand* current, T* p1, int length)
        {
            _p1 = p1;
            _current = current - 1;
            _end = current + length;
        }
        public SparseQueryEnumerator(NativeView<DrawEntityCommand> entities, NativeView<T> p1)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(entities.Length, p1.Length);

            _p1 = p1;
            _current = entities - 1;
            _end = entities + entities.Length;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_current < _end;

        public readonly VisibleQueryItem<T> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(ref *_current, ref _p1[_current->Index()]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly SparseQueryEnumerator<T> GetEnumerator() => this;
    }

    public ref struct SparseQueryEnumerator<T1, T2> where T1 : unmanaged where T2 : unmanaged
    {
        private readonly T1* _p1;
        private readonly T2* _p2;

        private DrawEntityCommand* _current;
        private readonly DrawEntityCommand* _end;

        public SparseQueryEnumerator(NativeView<DrawEntityCommand> entities, NativeView<T1> p1, NativeView<T2> p2)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(entities.Length, p1.Length);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(entities.Length, p2.Length);

            _p1 = p1;
            _p2 = p2;
            _current = entities - 1;
            _end = entities + entities.Length;
        }

        public SparseQueryEnumerator(DrawEntityCommand* entities, T1* p1, T2* p2, int length)
        {
            _p1 = p1;
            _p2 = p2;
            _current = entities - 1;
            _end = entities + length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_current < _end;

        public readonly VisibleQueryItem<T1, T2> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(ref *_current, ref _p1[_current->Index()], ref _p2[_current->Index()]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly SparseQueryEnumerator<T1, T2> GetEnumerator() => this;
    }

}