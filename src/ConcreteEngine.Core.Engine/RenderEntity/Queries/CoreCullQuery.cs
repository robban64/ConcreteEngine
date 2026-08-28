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

        private int _entity;
        private readonly uint _filter;

        public CullQueryEnumerator(NativeView<DrawPolicy> policies, NativeView<byte> visibilityMasks,
            NativeView<BoundingAxisBox> p1,
            EntityDrawStatus minStatus, PassMask passes)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(policies.Length, p1.Length);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(policies.Length, visibilityMasks.Length);

            _policy = policies.Ptr - 1;
            _end = policies.EndPtr;
            _visibilityMasks = visibilityMasks.Ptr - 1;
            _bounds = p1.Ptr - 1;
            _entity = 0;
            _filter = (ushort)((byte)minStatus | ((byte)passes << 8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_filter > 0)
            {
                while (++_policy < _end)
                {
                    ++_bounds;
                    ++_visibilityMasks;
                    ++_entity;

                    var filterStatus = (byte)_filter;
                    var filterPasses = (byte)(_filter >> 8);
                    if (((byte)_policy->Status >= filterStatus) | ((filterPasses & *_visibilityMasks) != 0))
                        return true;
                }
            }
            else
            {
                while (++_policy < _end)
                {
                    ++_bounds;
                    ++_visibilityMasks;
                    ++_entity;

                    var filterPasses = (byte)(_filter >> 8);
                    if ((filterPasses & *_visibilityMasks) != 0)
                        return true;
                }
            }


            return false;
        }

        public readonly CullQueryItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(new RenderEntityId(_entity), ref *_visibilityMasks, in *_policy, in *_bounds);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly CullQueryEnumerator GetEnumerator() => this;
    }
}