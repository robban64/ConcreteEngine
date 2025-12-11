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

    public List<EditorEntityResource> CreateEntityList()
    {
        const string animationName = "Animation";
        var entities = Entities;
        var result = new List<EditorEntityResource>(entities.EntityCount);

        foreach (var query in entities.CoreQuery())
        {
            ref readonly var source = ref query.Source;
            var entity = query.Entity;
            var item = new EditorEntityResource
            {
                Id = new EditorId(entity, EditorItemType.Entity),
                Generation = 0,
                Name = string.Empty,
                DisplayName = SourceNames[(int)source.Kind],
                Model = new EditorId(source.Model, EditorItemType.Model),
            };
            result.Add(item);
        }

        foreach (var query in entities.Query<ParticleComponent>())
        {
            ref readonly var comp = ref query.Component;
            result[query.CoreIndex].ComponentRef = new EditorId(comp.EmitterHandle, EditorItemType.Particle);
        }

        foreach (var query in entities.Query<AnimationComponent>())
        {
            ref readonly var comp = ref query.Component;
            result[query.CoreIndex].DisplayName = animationName;
            result[query.CoreIndex].ComponentRef = new EditorId(comp.Animation, EditorItemType.Animation);
        }

        return result;
    }

    public void LoadToEditor(EntityId entity, ref EntityDataState data)
    {
        if (entity == 0) return;
        var view = Entities.Core.GetEntityView(entity);
        data.Transform.From(in Transform.UnsafeAs(ref view.Transform));
        data.Bounds = view.Box.Bounds;
    }


    public void SaveToEngine(EntityId entity, in EntityDataState data)
    {
        var writer = Entities.Core.GetEntityWriter(entity);
        writer.Box.Bounds = data.Bounds;
        writer.Transform.Translation = data.Transform.Translation;
        writer.Transform.Rotation = data.Transform.Rotation;
        writer.Transform.Scale = data.Transform.Scale;
    }
}