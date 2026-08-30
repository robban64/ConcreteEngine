using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.Render.Queries;

public static unsafe partial class RenderCoreQuery
{
    public readonly ref struct CullQueryItem(EntityDrawStatus status, PassMask originalPasses ,ref PassMask visibilityMask, in BoundingAxisBox bounds)
    {
        public readonly EntityDrawStatus Status = status;
        public readonly PassMask OriginalPasses = originalPasses;
        public readonly ref PassMask VisibilityMask = ref visibilityMask;
        public readonly ref readonly BoundingAxisBox Bounds = ref bounds;
    }

    public readonly ref struct QueryItem<T1>(int entity, ref T1 item1) where T1 : unmanaged
    {
        public readonly int Entity = entity;
        public readonly ref T1 Item1 = ref item1;
    }

    public readonly ref struct QueryItem<T1, T2>(int entity, ref T1 item1, ref T2 item2)
        where T1 : unmanaged where T2 : unmanaged
    {
        public readonly int Entity = entity;
        public readonly ref T1 Item1 = ref item1;
        public readonly ref T2 Item2 = ref item2;
    }
}