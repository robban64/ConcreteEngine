using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Inspector;

namespace ConcreteEngine.Editor;

public abstract class SceneController
{
    public abstract int Count { get; }
    public abstract int GetCountByKind(SceneObjectKind kind);
    public abstract ReadOnlySpan<SceneObject> GetSceneObjectSpan();
    public abstract SceneObject GetSceneObject(SceneObjectId id);
    public abstract bool TryGetSceneObject(SceneObjectId id, out SceneObject asset);
    public abstract void SpawnSceneObject(Model model, in Transform transform);
}