using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.Render.Queries;

public static unsafe partial class RenderCoreQuery
{
    public ref struct VisibilityQueryEnumerator<T1> where T1 : unmanaged
    {
        private readonly PassMask* _end;

        private PassMask* _passes;
        private DrawPolicy* _policies;
        private T1* _p1;

        private int _entity;
        private readonly PassMask _filter;
        private PassMask _current;

        public VisibilityQueryEnumerator(NativeView<PassMask> passes, NativeView<DrawPolicy> policies,
            NativeView<T1> p1, PassMask mask)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(passes.Length, p1.Length);

            _end = passes.EndPtr;
            _passes = passes.Ptr - 1;
            _p1 = p1.Ptr - 1;
            _policies = policies.Ptr - 1;
            _entity = -1;
            _filter = mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_passes < _end)
            {
                ++_policies;
                ++_p1;
                ++_entity;

                _current = *_passes;
                var c = _current & _filter;
                if (c != 0) return true;
            }

            return false;
        }

        public readonly Item Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_entity, _current,  *_policies, ref *_p1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly VisibilityQueryEnumerator<T1> GetEnumerator() => this;

        public readonly ref struct Item(int entity, PassMask passes,  DrawPolicy policy, ref T1 item1)
        {
            public readonly int Entity = entity;
            public readonly PassMask Passes = passes;
            public readonly DrawQueue Queue = policy.Queue;
            public readonly ref T1 Item1 = ref item1;
        }
    }

}