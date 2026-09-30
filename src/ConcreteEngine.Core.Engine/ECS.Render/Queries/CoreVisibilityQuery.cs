using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.Render.Queries;

public static unsafe partial class RenderCoreQuery
{
    public ref struct VisibilityQueryEnumerator<T1> where T1 : unmanaged
    {
        private int _entity;
        private readonly int _length;
        private readonly BitSet _visibilitySet;
        private DrawPolicy* _policies;
        private T1* _p1;


        public VisibilityQueryEnumerator(BitSet visibilitySet, NativeView<DrawPolicy> policies, NativeView<T1> p1)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(policies.Length, p1.Length);
            _entity = -1;
            _length = policies.Length;
            _visibilitySet = visibilitySet;
            _policies = policies.Ptr - 1;
            _p1 = p1.Ptr - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_entity < _length)
            {
                ++_policies;
                ++_p1;
                return _visibilitySet.GetUnchecked(_entity);
            }

            return false;
        }

        public readonly Item Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_entity,  *_policies, ref *_p1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly VisibilityQueryEnumerator<T1> GetEnumerator() => this;

        public readonly ref struct Item(int entity, DrawPolicy policy, ref T1 item1)
        {
            public readonly int Entity = entity;
            public readonly DrawPolicy Policy = policy;
            public readonly ref T1 Item1 = ref item1;
        }
    }
    
}