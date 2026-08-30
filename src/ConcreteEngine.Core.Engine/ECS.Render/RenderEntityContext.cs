using System.Runtime.CompilerServices;
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
        public ref T GetComponent<T>() where T : unmanaged, IRenderComponent<T> 
            => ref RenderEcs.Store<T>().GetUnchecked(Entity.Id);
    }
}