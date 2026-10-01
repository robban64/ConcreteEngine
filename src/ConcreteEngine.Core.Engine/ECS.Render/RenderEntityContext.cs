using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.Render;

public sealed  partial class RenderEntityCore
{
    public readonly ref struct RenderEntityContext(RenderEntity entity, EntityDataStore core)
    {
        public readonly RenderEntity Entity = entity;
        public ref DrawSource Source => ref core.GetSource(Entity.Id);
        public ref DrawPolicy Policy => ref core.GetPolicy(Entity.Id);
        public ref BoundingAxisBox WorldBounds => ref core.GetWorldBounds(Entity.Id);
        public ref TransformUniform Transform => ref core.GetTransform(Entity.Id);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStatus(EntityDrawStatus status)
        {
            ref var policy = ref Policy;
            policy = policy.WithStatus(status);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToggleDrawFlag(EntityDrawFlags flag, bool enabled)
        {
            if (enabled) Source.DrawFlags |= flag;
            else Source.DrawFlags &= ~flag;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>() where T : unmanaged, IRenderComponent<T> 
            => ref RenderEcs.Store<T>().GetUnchecked(Entity.Id);
        
        public bool AddComponent<T>(T t) where T : unmanaged, IRenderComponent<T> 
            => RenderEcs.Store<T>().Add(Entity, in t);

        public bool RemoveComponent<T>() where T : unmanaged, IRenderComponent<T> 
            => RenderEcs.Store<T>().Remove(Entity);

    }
}