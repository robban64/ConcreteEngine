using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.Render;

public sealed partial class RenderEntityCore
{
    public sealed class EntityDataStore : IDisposable
    {
        public int Capacity { get; private set; }

        private BitSet _entitySet;
        private BitSet _visibleSet;

        private NativeArray<ushort> _generations;
        
        private NativeArray<DrawPolicy> _policies;
        private NativeArray<DrawSource> _sources;
        
        private NativeArray<BoundingAxisBox> _bounds;
        private NativeArray<TransformUniform> _transforms;
        
        private NativeArray<ulong> _drawKeys;

        internal EntityDataStore(int capacity)
        {
            Allocate(capacity);
        }

        public ref readonly BitSet VisibleSet => ref _visibleSet;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(int entity) => _entitySet[entity];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsVisible(int entity) => _visibleSet[entity];

        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetGeneration(int entity) => _generations[entity];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref DrawSource GetSource(int entity) => ref _sources[entity];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref DrawPolicy GetPolicy(int entity) => ref _policies[entity];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref BoundingAxisBox GetWorldBounds(int entity) => ref _bounds[entity];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TransformUniform GetTransform(int entity) => ref _transforms[entity];
        //

        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<DrawPolicy> PolicyView(int count) => _policies.Slice(0, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<DrawSource> SourceView(int count) => _sources.Slice(0, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<BoundingAxisBox> WorldBoundView(int count) => _bounds.Slice(0, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<TransformUniform> TransformView(int count) => _transforms.Slice(0, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<ulong> DrawKeyView(int count) => _drawKeys.Slice(0, count);
        //

        //
        internal RenderEntity AddEntity(int entity, DrawPolicy policy, DrawSource source)
        {
            if (_entitySet[entity]) Throwers.InvalidArgument("Entity already exists");
            _entitySet[entity] = true;
            _policies[entity] = policy;
            _sources[entity] = source;
            _bounds[entity] = default;

            ref var transform = ref _transforms[entity];
            transform.Model = Matrix4x4.Identity;
            transform.Normal = Matrix3X4.Identity;
            
            var gen = ++_generations[entity];
            return new RenderEntity(entity, gen);

        }

        internal void RemoveEntity(RenderEntity entity)
        {
            var generation = _generations[entity.Id];
            if(entity.Gen != generation) Throwers.InvalidArgument(nameof(entity), "Entity generation mismatch");
            _entitySet[entity.Id] = false;
            _sources[entity.Id] = default;
            _policies[entity.Id] = default;
        }


        //
        private void Allocate(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
            if (!_generations.IsNullOrEmpty) Throwers.InvalidOperation();
            
            _generations = NativeArray.Allocate<ushort>(capacity);
            _policies = NativeArray.Allocate<DrawPolicy>(capacity);
            _sources = NativeArray.Allocate<DrawSource>(capacity);
            _bounds = NativeArray.Allocate<BoundingAxisBox>(capacity);
            _transforms = NativeArray.Allocate<TransformUniform>(capacity);
            _drawKeys = NativeArray.Allocate<ulong>(capacity);
            
            _entitySet = new BitSet(capacity);
            _visibleSet = new BitSet(capacity);
            Capacity = capacity;
        }


        public void ReAlloc(int newSize)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(newSize, _policies.Length);
            _generations.ReAlloc(newSize, true);
            _policies.ReAlloc(newSize, true);
            _sources.ReAlloc(newSize, true);
            _drawKeys.ReAlloc(newSize, true);
            _bounds.ReAlloc(newSize, false);
            _transforms.ReAlloc(newSize, false);

            if (newSize > _entitySet.BitCount)
            {
                _entitySet = new BitSet(newSize);
                _visibleSet = new BitSet(newSize);
            }

            Capacity = newSize;
        }


        public void Dispose()
        {
            _generations.Dispose();
            _policies.Dispose();
            _sources.Dispose();
            _drawKeys.Dispose();
            _bounds.Dispose();
            _transforms.Dispose();
            Capacity = 0;
        }
    }
}