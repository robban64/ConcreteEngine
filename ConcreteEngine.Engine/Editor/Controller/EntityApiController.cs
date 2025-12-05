#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.ViewModel;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class EntityApiController(ApiContext apiContext)
{
    private WorldEntities Entities => apiContext.World.Entities;
    private static readonly string[] SourceNames = Enum.GetNames<RenderSourceKind>();

    //private Dictionary<ModelId, string> _modelToName = new(16);

    public List<EntityRecord> GetEntityList()
    {
        var entities = Entities;
        var result = new List<EntityRecord>(entities.EntityCount);
        var matTable = apiContext.World.GetMaterialTableImpl();
        var store = apiContext.AssetSystem.StoreImpl;

        foreach (var query in entities.CoreQuery())
        {
            var sourceName = SourceNames[(int)query.Source.Kind];
            int[] materials = [];
            if (matTable.TryResolveSubmitMaterial(query.Source.MaterialKey, out var tag))
                materials = MemoryMarshal.Cast<MaterialId, int>(tag.AsReadOnlySpan()).ToArray();
            
            result.Add(new EntityRecord(
                entityId: query.Entity,
                model: query.Source.Model,
                name: sourceName,
                materials: materials,
                componentCount: 0)
            );
        }

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