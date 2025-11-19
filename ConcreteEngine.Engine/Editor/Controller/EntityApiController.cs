using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.ViewModel;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Shared.TransformData;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class EntityApiController(ApiContext apiContext)
{
    private WorldEntities Entities => apiContext.World.Entities;
    
    public List<EntityRecord> GetEntityList()
    {
        var entities = Entities;
        var result = new List<EntityRecord>(entities.Models.Count);
        foreach (var it in entities.Query<ModelComponent>())
            result.Add(EditorObjectMapper.MakeEntityViewModel(it.Entity));

        result.Sort();
        return result;
    }
    
    public long FillEntityData(ref EntityDataPayload data)
    {
        var entities = Entities;

        if (data.EntityId == 0)
        {
            WorldActionSlot.SelectedEntityId = new EntityId(0);
            return 0;
        }

        var entity = new EntityId(data.EntityId);
        var model = entities.Models.GetById(entity);
        if (!entities.Transforms.TryGetById(entity, out var transform)) transform = default;
        if (!entities.BoundingBoxes.TryGetById(entity, out var bounds)) bounds = default;

        WorldActionSlot.SelectedEntityId = new EntityId(data.EntityId);

        data.Transform =
            new TransformData(in transform.Translation, in transform.Scale, in transform.Rotation);
        data.Model = new EditorEntityModel(model.Model, model.MaterialKey.Value, model.DrawCount);
        data.Bounds = bounds.Box;

        return data.EntityId;
    }
    
    public long WriteToEntity(long version,ref EntityDataPayload data)
    {
        WorldActionSlot.SelectedEntityId = new EntityId(data.EntityId);
        WorldActionSlot.SetSlot(version, in data);
        return data.EntityId;
    }


}