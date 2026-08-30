using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.Render.Queries;

public static unsafe partial class RenderCoreQuery
{

    public ref struct CullQueryEnumerator
    {
        private DrawPolicy* _policy;
        private readonly DrawPolicy* _end;

        private PassMask* _visibilityMasks;
        private BoundingAxisBox* _bounds;

        private EntityDrawStatus _currentStatus;
        private PassMask _currentPasses;

        private readonly EntityDrawStatus _minStatus;

        public CullQueryEnumerator(NativeView<DrawPolicy> policies, NativeView<PassMask> visibilityMasks,
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
                var status = *_policy;
                _currentPasses = _policy->Passes;
                _currentStatus = _policy->Status;
                if (_currentStatus >= _minStatus) return true;
            }

            return false;
        }

        public readonly CullQueryItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_currentStatus, _currentPasses, ref *_visibilityMasks, in *_bounds);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly CullQueryEnumerator GetEnumerator() => this;
    }
}