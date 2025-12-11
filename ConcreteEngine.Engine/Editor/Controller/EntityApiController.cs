#region

using System.Runtime.InteropServices;
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
            var entity = query.Entity;
            var isAnimated = entities.Animations.Has(entity);
            var item = new EditorEntityResource
            {
                Id = new EditorId(entity, EditorItemType.Entity),
                Generation = 0,
                Name = string.Empty,
                DisplayName = !isAnimated ? SourceNames[(int)source.Kind] : "Animated",
                Model = new EditorId(source.Model, EditorItemType.Model),
            };

            if (matTable.TryResolveSubmitMaterial(source.MaterialKey, out var tag))
            {
                var materialIds = tag.AsReadOnlySpan();
                if (materialIds.Length == 1)
                    item.Material = new EditorId(materialIds[0], EditorItemType.Material);
                else if (materialIds.Length > 1)
                {
                    var res = new EditorId[materialIds.Length];
                    for (var i = 0; i < materialIds.Length; i++)
                        res[i] = new EditorId(materialIds[i], EditorItemType.Material);

                    item.Materials = res;
                }
            }

            result.Add(item);
        }

        return result;
    }

    public void WriteSelectedEntity(EntityId entity, ref EntityDataState data)
    {
        if (entity == 0) return;
        var view = Entities.Core.GetEntityView(entity);
        data.EntityId = entity;
        data.Transform.From(in Transform.UnsafeAs(ref view.Transform));
        data.Bounds = view.Box.Bounds;
    }


    public void ApplySelectedEntity(EntityId entity, in EntityDataState data)
    {
        var writer = Entities.Core.GetEntityWriter(entity);
        writer.Box.Bounds = data.Bounds;
        writer.Transform.Translation = data.Transform.Translation;
        writer.Transform.Rotation = data.Transform.Rotation;
        writer.Transform.Scale = data.Transform.Scale;

    }
}