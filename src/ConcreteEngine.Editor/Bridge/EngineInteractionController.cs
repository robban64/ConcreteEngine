using System.Numerics;
using ConcreteEngine.Core.Engine;

namespace ConcreteEngine.Editor.Bridge;

public abstract class EngineInteractionController
{
    public abstract SceneObjectId Raycast(Vector2 mousePos);
    public abstract Vector3 RaycastEntityOnTerrain(SceneObjectId id, Vector2 mousePos, Vector3 origin);
    public abstract Vector3 RaycastTerrain(Vector2 mousePos);
}