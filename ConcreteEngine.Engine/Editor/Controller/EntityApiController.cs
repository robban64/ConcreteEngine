#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Editor.Components.Data;
using ConcreteEngine.Editor.Components.State;
using ConcreteEngine.Editor.Data;
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