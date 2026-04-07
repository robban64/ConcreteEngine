using System.Numerics;
using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Editor;

public abstract class InteractionController
{
    public abstract SceneObjectId Raycast(Vector2 mousePos);
    public abstract Vector3 RaycastEntityOnTerrain(SceneObjectId id, Vector2 mousePos, Vector3 origin);
    public abstract Vector3 RaycastTerrain(Vector2 mousePos);
}