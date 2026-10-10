using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Render;

public sealed partial class RenderWorld
{
    public sealed class RenderMetaStore : IDisposable
    {
        public const int MinCapacity = 128;
        
        private ushort[] _generations;

        private BitSet _entitySet;
        private BitSet _visibleSet;

        private BitSet _trueSet;

        internal RenderMetaStore(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(capacity, MinCapacity);

            _generations = new ushort[capacity];

            _entitySet = new BitSet(capacity);
            _visibleSet = new BitSet(capacity);
            _trueSet = new BitSet(capacity);
            _trueSet.SetAll(true);
        }

        //
        public BitSet EntitySet => _entitySet;
        public BitSet VisibleSet => _visibleSet;
        public BitSet TrueSet => _trueSet;

        public ReadOnlySpan<ushort> GenerationSpan() => new(_generations, 0, EntityCount);

        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetGeneration(int entity) => _generations[entity];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(int entity) => _entitySet[entity];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsVisible(int entity) => _visibleSet[entity];


        //
        internal RenderEntity AddEntity(int entity, DrawPolicy policy, DrawSource source)
        {
            if (_entitySet[entity]) Throwers.InvalidArgument("Entity already exists");
            _entitySet[entity] = true;

            Dense<DrawPolicy>()[entity] = policy;
            Dense<DrawSource>()[entity] = source;
            Dense<WorldBox>()[entity] = default;
            Dense<WorldTransform>()[entity].Transform = Matrix4x4.Identity;
            Dense<WorldNormal>()[entity].Normal = Matrix3X4.Identity;

            var gen = ++_generations[entity];
            return new RenderEntity(entity, gen);
        }

        internal void RemoveEntity(RenderEntity entity)
        {
            if (!IsAlive(entity.Id)) Throwers.InvalidArgument("Bug: Entity is already dead", nameof(entity));

            var generation = _generations[entity.Id];
            if (entity.Gen != generation) Throwers.InvalidArgument(nameof(entity), "Bug: Entity generation mismatch");
            _entitySet[entity.Id] = false;
            Dense<DrawPolicy>()[entity.Id] = default;
            Dense<DrawSource>()[entity.Id] = default;
        }


        //
        internal bool Resize(int newSize)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(newSize, EntityCapacity);

            Array.Resize(ref _generations, newSize);

            if (newSize > _entitySet.BitCapacity)
            {
                _entitySet = _entitySet.Resized(newSize);
                _visibleSet = _visibleSet.Resized(newSize);
                _trueSet = _trueSet.Resized(newSize);
                _trueSet.SetAll(true);
                return true;
            }
            return false;

        }


        public void Dispose()
        {
        }

    }
}