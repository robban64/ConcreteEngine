using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.RenderEntity.Queries;

public static unsafe partial class RenderCoreQuery
{
    public readonly ref struct CullQueryItem(ref byte visibilityMask, in DrawPolicy policy, in BoundingAxisBox bounds)
    {
        public readonly ref byte VisibilityMask = ref visibilityMask;
        public readonly ref readonly DrawPolicy Policy = ref policy;
        public readonly ref readonly BoundingAxisBox Bounds = ref bounds;
    }

    public readonly ref struct QueryItem<T1>(RenderEntityId entity, ref T1 item1) where T1 : unmanaged
    {
        public readonly RenderEntityId Entity = entity;
        public readonly ref T1 Item1 = ref item1;
    }

    public readonly ref struct QueryItem<T1, T2>(RenderEntityId entity, ref T1 item1, ref T2 item2)
        where T1 : unmanaged where T2 : unmanaged
    {
        public readonly RenderEntityId Entity = entity;
        public readonly ref T1 Item1 = ref item1;
        public readonly ref T2 Item2 = ref item2;
    }
}