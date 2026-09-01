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

        private PassMask* _drawPasses;
        private BoundingAxisBox* _bounds;

        private PassMask _currentPasses;
        private EntityDrawStatus _currentStatus;

        private readonly EntityDrawStatus _minStatus;

        public CullQueryEnumerator(NativeView<DrawPolicy> policies, NativeView<PassMask> drawPasses,
            NativeView<BoundingAxisBox> p1, EntityDrawStatus minStatus)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(policies.Length, p1.Length);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(policies.Length, drawPasses.Length);

            _policy = policies.Ptr - 1;
            _end = policies.EndPtr;
            _drawPasses = drawPasses.Ptr - 1;
            _bounds = p1.Ptr - 1;
            _minStatus = minStatus;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_policy < _end)
            {
                ++_drawPasses;
                ++_bounds;
                var policy = *_policy;
                if (policy.Status >= _minStatus)
                {
                    _currentPasses = policy.Passes;
                    _currentStatus = policy.Status;
                    return true;
                }
            }

            return false;
        }

        public readonly CullQueryItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_currentStatus, _currentPasses, ref *_drawPasses, in *_bounds);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly CullQueryEnumerator GetEnumerator() => this;
    }
}