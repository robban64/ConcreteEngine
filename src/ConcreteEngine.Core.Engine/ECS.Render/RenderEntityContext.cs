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
            var newPolicy = policy.WithStatus(status);
            policy = newPolicy;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>() where T : unmanaged, IRenderComponent<T> 
            => ref RenderEcs.Store<T>().GetUnchecked(Entity.Id);
    }
    
    public readonly ref struct RenderEntityHandleContext(NativeView<ushort> generations)
    {
        private readonly NativeView<ushort> _generations = generations;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RenderEntity MakeHandle(int entityId)
        {
            if((uint)entityId >= (uint)_generations.Length) Throwers.InvalidArgument(nameof(entityId));
            return new RenderEntity(entityId, _generations[entityId]);
        }
    }

}