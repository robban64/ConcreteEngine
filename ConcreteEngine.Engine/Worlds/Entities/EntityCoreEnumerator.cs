#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Entities.Components;

#endregion

namespace ConcreteEngine.Engine.Worlds.Entities;

internal ref struct EntityCoreEnumerator(EntityCoreStore r)
{
    private int _i = -1;
    public bool MoveNext() => ++_i < r.Count;
    public EntityCoreQuery Current => new(_i, r);

    public EntityCoreEnumerator GetEnumerator()
    {
        _i = -1;
        return this;
    }

    internal readonly ref struct EntityCoreQuery(int idx, EntityCoreStore r)
    {
        public readonly int Index = idx;
        public readonly EntityId Entity = new(idx + 1);
        public ref RenderSourceComponent Source => ref r.GetSourceById(Entity);
        public ref Transform Transform => ref r.GetTransformById(Entity);
        public ref BoxComponent Box => ref r.GetBoxById(Entity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillTransformBox(out Transform transform, out BoundingBox box)
        {
            transform = Transform;
            box = Box.Bounds;
        }
    }
}