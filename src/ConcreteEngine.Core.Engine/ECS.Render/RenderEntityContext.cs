using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.Render;

public sealed  partial class RenderEntityCore
{
    public readonly ref struct RenderEntityContext(int entity, EntityDataStore core)
    {
        public readonly int Entity = entity;
        public ref DrawSource Source => ref core.GetSource(Entity);
        public ref DrawPolicy Policy => ref core.GetDrawPolicy(Entity);
        public ref BoundingAxisBox WorldBounds => ref core.GetWorldBounds(Entity);
        public ref TransformUniform Transform => ref core.GetTransformData(Entity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>() where T : unmanaged, IRenderComponent<T> 
            => ref RenderEcs.Store<T>().GetUnchecked(Entity);
    }
}