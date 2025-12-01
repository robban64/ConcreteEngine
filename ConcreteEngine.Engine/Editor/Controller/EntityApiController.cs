#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.ViewModel;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class EntityApiController(ApiContext apiContext)
{
    private WorldEntities Entities => apiContext.World.Entities;

    public List<EntityRecord> GetEntityList()
    {
        var entities = Entities;
        var result = new List<EntityRecord>(entities.EntityCount);
        foreach (var it in entities.Core.GetEntitySpan())
            result.Add(EditorObjectMapper.MakeEntityViewModel(it));

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
        var view = entities.Core.GetEntityView(entity);
        if (!entities.BoundingBoxes.TryGetById(entity, out var bounds)) bounds = default;

        WorldActionSlot.SelectedEntityId = new EntityId(data.EntityId);

        data.Transform = Transform.AsData(ref view.Transform);
        data.Model = new EditorEntityModel(view.Model.Model, view.Model.MaterialKey.Value, view.Model.DrawCount);
        data.Bounds = bounds.Box;

        return data.EntityId;
    }

    public long WriteToEntity(long version, ref EntityDataPayload data)
    {
        WorldActionSlot.SelectedEntityId = new EntityId(data.EntityId);
        WorldActionSlot.SetSlot(version, in data);
        return data.EntityId;
    }
}