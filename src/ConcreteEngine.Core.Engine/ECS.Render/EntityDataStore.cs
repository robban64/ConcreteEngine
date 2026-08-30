using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.Render;

public sealed partial class RenderEntityCore
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref DrawSource GetSource(RenderEntity e) => ref _entityDataStore.GetSource(e.Entity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref DrawPolicy GetDrawPolicy(RenderEntity e) => ref _entityDataStore.GetDrawPolicy(e.Entity);
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref BoundingAxisBox GetWorldBounds(RenderEntity e) => ref _entityDataStore.GetWorldBounds(e.Entity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Matrix4x4 GetModelMatrix(RenderEntity e) => ref _entityDataStore.GetModelMatrix(e.Entity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Matrix3X4 GetNormalMatrix(RenderEntity e) => ref _entityDataStore.GetNormalMatrix(e.Entity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TransformUniform GetTransformData(RenderEntity e) => ref _entityDataStore.GetTransformData(e.Entity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<PassMask> GetVisibilityView() => _entityDataStore.GetVisibilityView();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<DrawPolicy> GetDrawPolicyView() => _entityDataStore.GetDrawPolicyView();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<DrawSource> GetSourceView() => _entityDataStore.GetSourceView();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<BoundingAxisBox> GetWorldBoundView() => _entityDataStore.GetWorldBoundView();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<TransformUniform> GetTransformView() => _entityDataStore.GetTransformView();

    public sealed class EntityDataStore : IDisposable
    {
        private NativeArray<PassMask> _passes;
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
        public ref DrawPolicy GetDrawPolicy(int entity) => ref _policies[entity];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref BoundingAxisBox GetWorldBounds(int entity) => ref _bounds[entity];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Matrix4x4 GetModelMatrix(int entity) => ref _transforms[entity].Model;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Matrix3X4 GetNormalMatrix(int entity) => ref _transforms[entity].Normal;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TransformUniform GetTransformData(int entity) => ref _transforms[entity];
        //

        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<PassMask> GetVisibilityView() => _passes.Slice(0, RenderEcs.EntityCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<DrawPolicy> GetDrawPolicyView() => _policies.Slice(0, RenderEcs.EntityCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<DrawSource> GetSourceView() => _sources.Slice(0, RenderEcs.EntityCount);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<BoundingAxisBox> GetWorldBoundView() => _bounds.Slice(0, RenderEcs.EntityCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<TransformUniform> GetTransformView() => _transforms.Slice(0, RenderEcs.EntityCount);

        //

        //
        public void AddEntity(RenderEntity e, DrawPolicy policy, DrawSource source)
        {
            if (_policies[e.Entity].Status != 0) Throwers.InvalidArgument("Entity already exists");
            _policies[e.Entity] = policy;
            _sources[e.Entity] = source;
            ClearSpatial(e);
        }

        public void ClearHeader(RenderEntity e)
        {
            _passes[e.Entity] = 0;
            _sources[e.Entity] = default;
            _policies[e.Entity] = default;
        }

        public void ClearSpatial(RenderEntity e)
        {
            ref var transform = ref _transforms[e.Entity];
            transform.Model = Matrix4x4.Identity;
            transform.Normal = Matrix3X4.Identity;
            _bounds[e.Entity] = default;
        }

        //
        private void Allocate(int capacity)
        {
            _passes = NativeArray.Allocate<PassMask>(capacity);
            _policies = NativeArray.Allocate<DrawPolicy>(capacity);
            _sources = NativeArray.Allocate<DrawSource>(capacity);
            _bounds = NativeArray.Allocate<BoundingAxisBox>(capacity);
            _transforms = NativeArray.Allocate<TransformUniform>(capacity);
        }


        public void ReAlloc(int newSize)
        {
            ArgumentOutOfRangeException.ThrowIfEqual(newSize, _passes.Length);
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