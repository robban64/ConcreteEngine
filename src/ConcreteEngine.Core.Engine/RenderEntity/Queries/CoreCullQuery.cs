using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.RenderEntity.Queries;

public static unsafe partial class RenderCoreQuery
{

    public ref struct CullQueryEnumerator
    {
        private DrawPolicy* _policy;
        private readonly DrawPolicy* _end;

        private byte* _visibilityMasks;
        private BoundingAxisBox* _bounds;

        private readonly EntityDrawStatus _minStatus;

        public CullQueryEnumerator(NativeView<DrawPolicy> policies, NativeView<byte> visibilityMasks,
            NativeView<BoundingAxisBox> p1, EntityDrawStatus minStatus)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(policies.Length, p1.Length);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(policies.Length, visibilityMasks.Length);

            _policy = policies.Ptr - 1;
            _end = policies.EndPtr;
            _visibilityMasks = visibilityMasks.Ptr - 1;
            _bounds = p1.Ptr - 1;
            _minStatus = minStatus;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_policy < _end)
            {
                ++_visibilityMasks;
                ++_bounds;
                var status = _policy->Status;
                if (status >= _minStatus) return true;
            }

            return false;
        }

        public readonly CullQueryItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(ref *_visibilityMasks, in *_policy, in *_bounds);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly CullQueryEnumerator GetEnumerator() => this;
    }
}