using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Engine.ECS.Render.Queries;

public static unsafe partial class RenderCoreQuery
{
    public ref struct VisibilityQueryEnumerator<T1> where T1 : unmanaged
    {
        private int _entity;
        private readonly int _length;
        private readonly BitSet _visibilitySet;
        private T1* _p1;


        public VisibilityQueryEnumerator(BitSet visibilitySet, NativeView<T1> p1)
        {
            if(p1.IsNullOrEmpty) Throwers.InvalidArgument(nameof(p1));
            _entity = -1;
            _length = p1.Length;
            _visibilitySet = visibilitySet;
            _p1 = p1.Ptr - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_entity < _length)
            {
                ++_p1;
                if (_visibilitySet.GetUnchecked(_entity)) return true;
            }

            return false;
        }

        public readonly Item Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_entity, ref *_p1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly VisibilityQueryEnumerator<T1> GetEnumerator() => this;

        public readonly ref struct Item(int entity, ref T1 item1)
        {
            public readonly int Entity = entity;
            public readonly ref T1 Item1 = ref item1;
        }
    }
    
}