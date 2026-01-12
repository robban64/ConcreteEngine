using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Scene;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class SceneApiController(ApiContext context) : SceneController
{
    private readonly SceneManager _sceneManager = context.SceneManager;
    private readonly SceneStore _sceneStore = context.SceneManager.Store;

    public override ReadOnlySpan<ISceneObject> GetSceneObjectSpan() => _sceneManager.Store.GetSceneObjectSpan();
    public override ISceneObject GetSceneObject(SceneObjectId id) => _sceneManager.Store.Get(id);

    public override void Select(SceneObjectId id)
    {
        var sceneObject = _sceneManager.Store.Get(id);
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
        var sceneObject = _sceneManager.Store.Get(id);
        if (sceneObject == null!) return null!;
        var entity = sceneObject.GetRenderEntities()[0]; // wip just to test things
        var props = new List<ProxyPropertyEntry>(4);

        props.Add(ProxyPropertyFactory.CreateSpatialProperty(id));
        props.Add(ProxyPropertyFactory.CreateSourceProperty(entity));

        if (Ecs.Render.Stores<RenderAnimationComponent>.Store.Has(entity))
            props.Add(ProxyPropertyFactory.CreateAnimationProperty(entity));

        if (Ecs.Render.Stores<ParticleComponent>.Store.Has(entity))
            props.Add(ProxyPropertyFactory.CreateParticleProperty(entity));

        return new EditorSceneObjectProxy(sceneObject) { Properties = props };
    }
}