using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Proxy;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Scene;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class SceneApiController(ApiContext context) : SceneController
{
    private readonly SceneStore _sceneStore = context.SceneManager.Store;

    public override int Count => _sceneStore.Count;

    public override int GetCountByKind(SceneObjectKind kind)
    {
        return kind == SceneObjectKind.Empty ? _sceneStore.Count : _sceneStore.GetCountBy(kind);
    }

    public override SceneObjectHeader GetSceneObjectHeader(int index)
    {
        var it = _sceneStore.GetByIndex(index);
        return new SceneObjectHeader(it.Name, it.GId, it.Id, it.Enabled, it.Kind);
    }


    public override void Select(SceneObjectId id)
    {
        var sceneObject = _sceneStore.Get(id);
        foreach (var entity in sceneObject.GetRenderEntities())
        {
            if (Ecs.Render.Stores<SelectionComponent>.Store.Has(entity)) continue;
            Ecs.Render.Stores<SelectionComponent>.Store.Add(entity, new SelectionComponent());
        }
    }

    public override void Deselect(SceneObjectId id)
    {
        int len = Ecs.Render.Stores<SelectionComponent>.Store.ActiveCount;
        if (len == 0) return;
        Span<RenderEntityId> renderEntities = stackalloc RenderEntityId[len + 1];

        int idx = 0;
        foreach (var query in Ecs.Render.Query<SelectionComponent>())
            renderEntities[idx++] = query.RenderEntity;

        foreach (var it in renderEntities.Slice(0, idx))
            Ecs.Render.Stores<SelectionComponent>.Store.Remove(it);
    }


    public override SceneObjectProxy GetProxy(SceneObjectId id)
    {
        var sceneObject = _sceneStore.Get(id);
        if (sceneObject == null!) return null!;
        var entity = sceneObject.GetRenderEntities()[0]; // wip just to test things

        AnimationProperty? animation = null;
        if (Ecs.Render.Stores<RenderAnimationComponent>.Store.Has(entity))
            animation = SceneObjectProxyFactory.CreateAnimationProperty(entity);

        ParticleProperty? particle = null;
        if (Ecs.Render.Stores<ParticleComponent>.Store.Has(entity))
            particle = SceneObjectProxyFactory.CreateParticleProperty(entity);

        return new SceneObjectProxy(sceneObject.Id, sceneObject.Name, new SceneProxyProperties
        {
            SourceProperty = SceneObjectProxyFactory.CreateSourceProperty(entity),
            SpatialProperty = SceneObjectProxyFactory.CreateSpatialProperty(id),
            AnimationProperty = animation,
            ParticleProperty = particle,
        });
    }
}