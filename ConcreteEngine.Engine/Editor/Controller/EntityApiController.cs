#region

using System.Runtime.InteropServices;
using ConcreteEngine.Editor.Components.Data;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class EntityApiController(ApiContext apiContext)
{
    private WorldEntities Entities => apiContext.World.Entities;
    private static readonly string[] SourceNames = Enum.GetNames<RenderSourceKind>();

    //private Dictionary<ModelId, string> _modelToName = new(16);

    public List<EditorEntityResource> CreateEntityList()
    {
        var entities = Entities;
        var result = new List<EditorEntityResource>(entities.EntityCount);
        var matTable = apiContext.World.GetMaterialTableImpl();

        foreach (var query in entities.CoreQuery())
        {
            ref readonly var source = ref query.Source;
            var item = new EditorEntityResource
            {
                Id = new EditorId(query.Entity, EditorItemType.Entity),
                Generation = 0,
                Name = string.Empty,
                DisplayName = SourceNames[(int)source.Kind],
                Model = source.Model,
            };
            
            if (matTable.TryResolveSubmitMaterial(source.MaterialKey, out var tag))
            {
                var materialIds = tag.AsReadOnlySpan();
                if (materialIds.Length == 1) 
                    item.Material = materialIds[0];
                else if(materialIds.Length > 1)
                    MemoryMarshal.Cast<MaterialId, int>(tag.AsReadOnlySpan()).ToArray();
            }
           
            result.Add(item);
        }

        return result;
    }
    
    public void WriteSelectedEntity(EntityId entity , ref EntityDataState data)
    {
        if (entity == 0)
        {
            WorldActionSlot.SelectedEntityId = new EntityId(0);
            return;
        }

        WorldActionSlot.SelectedEntityId =  entity;
        
        var view = Entities.Core.GetEntityView(entity);
        data.EntityId = entity;
        data.Transform.From(in Transform.UnsafeAs(ref view.Transform));
        data.Bounds = view.Box.Bounds;
    }


    public void ApplySelectedEntity(EntityId entity, in EntityDataState data)
    {
        var writer = Entities.Core.GetEntityWriter(entity);
        writer.Box.Bounds = data.Bounds;
        data.Transform.FillData(out Transform.UnsafeAs(ref writer.Transform));
    }
}