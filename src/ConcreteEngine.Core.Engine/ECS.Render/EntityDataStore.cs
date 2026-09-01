using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.Render;

public sealed partial class RenderEntityCore
{
    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<PassMask> VisibilityView() => _entityDataStore.VisibilityView();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<DrawPolicy> PolicyView() => _entityDataStore.PolicyView();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<DrawSource> SourceView() => _entityDataStore.SourceView();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<BoundingAxisBox> WorldBoundView() => _entityDataStore.WorldBoundView();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<TransformUniform> TransformView() => _entityDataStore.TransformView();
    //

    public sealed class EntityDataStore : IDisposable
    {
        private NativeArray<byte> _passes;
        private NativeArray<DrawPolicy> _policies;
        private NativeArray<DrawSource> _sources;
        private NativeArray<BoundingAxisBox> _bounds;
        private NativeArray<TransformUniform> _transforms;

        internal EntityDataStore(int capacity)
        {
            Allocate(capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(int entity) => _policies[entity].Status != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsVisible(int entity) => _passes[entity] != 0;

        //
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
        public NativeView<PassMask> VisibilityView() => _passes.Slice(0, RenderEcs.EntityCount).Reinterpret<PassMask>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<DrawPolicy> PolicyView() => _policies.Slice(0, RenderEcs.EntityCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<DrawSource> SourceView() => _sources.Slice(0, RenderEcs.EntityCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<BoundingAxisBox> WorldBoundView() => _bounds.Slice(0, RenderEcs.EntityCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<TransformUniform> TransformView() => _transforms.Slice(0, RenderEcs.EntityCount);

        //

        //
        internal void AddEntity(RenderEntity e, DrawPolicy policy, DrawSource source)
        {
            if (_policies[e.Id].Status != 0) Throwers.InvalidArgument("Entity already exists");
            _policies[e.Id] = policy;
            _sources[e.Id] = source;
            ClearSpatial(e);
        }

        internal void ClearHeader(RenderEntity e)
        {
            _passes[e.Id] = 0;
            _sources[e.Id] = default;
            _policies[e.Id] = default;
        }

        internal void ClearSpatial(RenderEntity e)
        {
            ref var transform = ref _transforms[e.Id];
            transform.Model = Matrix4x4.Identity;
            transform.Normal = Matrix3X4.Identity;
            _bounds[e.Id] = default;
        }

        //
        private void Allocate(int capacity)
        {
            if (!_policies.IsNullOrEmpty) Throwers.InvalidOperation();
            _passes = NativeArray.Allocate<byte>(capacity);
            _policies = NativeArray.Allocate<DrawPolicy>(capacity);
            _sources = NativeArray.Allocate<DrawSource>(capacity);
            _bounds = NativeArray.Allocate<BoundingAxisBox>(capacity);
            _transforms = NativeArray.Allocate<TransformUniform>(capacity);
        }


        public void ReAlloc(int newSize)
        {
            ArgumentOutOfRangeException.ThrowIfEqual(newSize, _policies.Length);
            _passes.ReAlloc(newSize, true);
            _policies.ReAlloc(newSize, true);
            _sources.ReAlloc(newSize, true);
            _bounds.ReAlloc(newSize, false);
            _transforms.ReAlloc(newSize, false);
        }


        public void Dispose()
        {
            _passes.Dispose();
            _policies.Dispose();
            _sources.Dispose();
            _bounds.Dispose();
            _transforms.Dispose();
        }
    }
}