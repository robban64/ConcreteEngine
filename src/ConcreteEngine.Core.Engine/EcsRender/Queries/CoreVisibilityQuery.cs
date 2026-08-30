using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.EcsRender.Queries;

public static unsafe partial class RenderCoreQuery
{
    public ref struct VisibilityQueryEnumerator<T1> where T1 : unmanaged
    {
        private byte* _visibilityMasks;
        private T1* _p1;
        private readonly byte* _end;

        private int _entity;
        private readonly byte _filter;

        public VisibilityQueryEnumerator(NativeView<byte> visibilityMasks, NativeView<T1> p1, PassMask mask)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(visibilityMasks.Length, p1.Length);

            _visibilityMasks = visibilityMasks.Ptr - 1;
            _end = visibilityMasks.EndPtr;
            _p1 = p1.Ptr - 1;
            _entity = -1;
            _filter = (byte)mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_visibilityMasks < _end)
            {
                ++_p1;
                ++_entity;
                var c = *_visibilityMasks & _filter;
                if (c != 0) return true;
            }

            return false;
        }

        public readonly QueryItem<byte, T1> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(new RenderEntityIndex(_entity), ref *_visibilityMasks, ref *_p1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly VisibilityQueryEnumerator<T1> GetEnumerator() => this;
    }
}