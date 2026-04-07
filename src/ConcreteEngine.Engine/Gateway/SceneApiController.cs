using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Engine.Scene;

namespace ConcreteEngine.Engine.Gateway;

internal sealed class SceneApiController(SceneManager sceneManager) : SceneController
{
    private readonly SceneStore _sceneStore = sceneManager.Store;

    public override int Count => _sceneStore.Count;

    public override void SpawnSceneObject(Model model, in Transform transform) =>
        sceneManager.SpawnFrom(model, in transform);

    public override ReadOnlySpan<SceneObject> GetSceneObjectSpan() => _sceneStore.GetSceneObjectSpan();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override SceneObject GetSceneObject(SceneObjectId id) => _sceneStore.Get(id);

    public override bool TryGetSceneObject(SceneObjectId id, out SceneObject asset) =>
        _sceneStore.TryGet(id, out asset);

    public override int GetCountByKind(SceneObjectKind kind)
    {
        return kind == SceneObjectKind.Empty ? _sceneStore.Count : _sceneStore.GetCountBy(kind);
    }

    public override void ToggleDrawBounds(SceneObjectId id, bool enabled)
    {
        var sceneObject = _sceneStore.Get(id);
        var store = Ecs.Render.Stores<DebugBoundsComponent>.Store;
        foreach (var it in sceneObject.GetRenderEntities())
        {
            var isEnabled = store.Has(it);
            if (isEnabled && !enabled)
            {
                store.Remove(it);
            }
            else if (!isEnabled && enabled)
            {
                store.Add(it, new DebugBoundsComponent());
            }
        }
    }


    public override InspectSceneObject Select(SceneObjectId id)
    {
        var sceneObject = _sceneStore.Get(id);
        var hasDebugBounds = false;
        foreach (var entity in sceneObject.GetRenderEntities())
        {
            if (Ecs.Render.Stores<SelectionComponent>.Store.Has(entity)) continue;
            Ecs.Render.Stores<SelectionComponent>.Store.Add(entity, new SelectionComponent());

            if (Ecs.Render.Stores<DebugBoundsComponent>.Store.Has(entity))
                hasDebugBounds = true;
        }

        return new InspectSceneObject(sceneObject) { ShowDebugBounds = hasDebugBounds };
    }

    public override void Deselect(SceneObjectId id)
    {
        if (!_sceneStore.TryGet(id, out var sceneObject)) return;

        foreach (var entity in sceneObject.GetRenderEntities())
        {
            if (Ecs.Render.Stores<SelectionComponent>.Store.Has(entity))
                Ecs.Render.Stores<SelectionComponent>.Store.Remove(entity);

            if (Ecs.Render.Stores<DebugBoundsComponent>.Store.Has(entity))
                Ecs.Render.Stores<DebugBoundsComponent>.Store.Remove(entity);
        }
    }
}