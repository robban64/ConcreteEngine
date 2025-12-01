#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.ViewModel;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
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

        WorldActionSlot.SelectedEntityId = new EntityId(data.EntityId);

        data.Transform = Transform.UnsafeAs(ref view.Transform);
        data.Model = new EditorEntityModel(view.Source.Model, view.Source.MaterialKey.Value, view.Source.DrawCount);
        data.Bounds = view.Box;

        return data.EntityId;
    }

    public long WriteToEntity(long version, ref EntityDataPayload data)
    {
        WorldActionSlot.SelectedEntityId = new EntityId(data.EntityId);
        WorldActionSlot.SetSlot(version, in data);
        return data.EntityId;
    }
}