using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Engine.Scene;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class SceneApiController(ApiContext context) : IEngineSceneController
{
    private readonly Scene.Scene _scene = context.Scene;

    public ReadOnlySpan<ISceneObject> GetSceneObjectSpan() => _scene.Store.GetSceneObjectSpan();
    public ISceneObject GetSceneObject(SceneObjectId id) => _scene.Store.Get(id);
}