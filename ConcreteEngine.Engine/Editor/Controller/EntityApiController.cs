#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Editor.Components.Data;
using ConcreteEngine.Editor.Components.State;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render.Data;
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

    public void ProcessEntityRequest(ref EditorDataRequest<EntityDataState> request)
    {
        var entities = Entities;

        if (request.EditorData.EntityId == 0)
        {
            WorldActionSlot.SelectedEntityId = new EntityId(0);
            request.EditorData = default;
            request.ResponseStatus = EditorDataRequestStatus.Overwrite;
            return;
        }

        var entity = new EntityId(request.EditorData.EntityId);

        WorldActionSlot.SelectedEntityId =  entity;

        if (request.WriteRequest)
        {
            WorldActionSlot.SetSlot(0, in request.EditorData);
            request.ResponseStatus = EditorDataRequestStatus.Success;
            return;
        }
        
        var view = entities.Core.GetEntityView(entity);
        ref var data = ref request.EditorData;
        data.EntityId = entity;
        data.MaterialTagKey = view.Source.MaterialKey.Value;
        data.ModelId = view.Source.Model;
        data.SetTransform(Transform.UnsafeAs(ref view.Transform));
        data.Bounds = view.Box.Bounds;
        request.ResponseStatus = EditorDataRequestStatus.Success;
    }
}