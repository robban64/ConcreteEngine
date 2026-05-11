namespace ConcreteEngine.Core.Engine.Scene;

public abstract class BlueprintFactory
{
    public abstract SceneObject BuildSceneObject(SceneObjectId id, SceneObjectTemplate template);
}