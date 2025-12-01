using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;

namespace ConcreteEngine.Engine.Worlds.Render.Data;

public readonly struct DrawEntitySource(int id, MaterialTagKey materialTagKey, RenderSourceType sourceType)
{
    public readonly int Id = id;
    public readonly MaterialTagKey MaterialKey = materialTagKey;
    public readonly RenderSourceType SourceType = sourceType;

    public static implicit operator RenderSourceComponent(DrawEntitySource t) => new(t.Id, t.MaterialKey, t.SourceType);
    public static implicit operator DrawEntitySource(RenderSourceComponent d) => new(d.Id, d.MaterialKey, d.SourceType);
    
    public ref T GetSourceType<T>(EntityId entityId) where T : unmanaged, IRenderSourceComponent
    {
        return ref WorldEntities.GetStore<T>().GetById(entityId);
    }
}