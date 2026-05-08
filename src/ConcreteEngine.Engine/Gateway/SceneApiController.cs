using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor;
using ConcreteEngine.Engine.Scene;

namespace ConcreteEngine.Engine.Gateway;

internal sealed class SceneApiController(SceneManager sceneManager) : SceneController
{
    private readonly SceneStore _sceneStore = sceneManager.Store;

    public override int Count => _sceneStore.Count;


    public override ReadOnlySpan<SceneObject> GetSceneObjectSpan() => _sceneStore.GetSceneObjectSpan();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override SceneObject GetSceneObject(SceneObjectId id) => _sceneStore.Get(id);

    public override bool TryGetSceneObject(SceneObjectId id, out SceneObject asset) =>
        _sceneStore.TryGet(id, out asset);

    public override int GetCountByKind(SceneObjectKind kind)
    {
        return kind == SceneObjectKind.Empty ? _sceneStore.Count : _sceneStore.GetCountBy(kind);
    }

    public override void SpawnSceneObject(Model model, in Transform transform) =>
        sceneManager.SpawnFrom(model, in transform);
}