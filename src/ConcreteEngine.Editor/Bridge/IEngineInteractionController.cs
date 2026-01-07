using System.Numerics;
using ConcreteEngine.Core.Engine;

namespace ConcreteEngine.Editor.Bridge;

public interface IEngineInteractionController
{
    SceneObjectId Raycast(Vector2 mousePos);
    Vector3 RaycastEntityOnTerrain(SceneObjectId id, Vector2 mousePos, Vector3 origin);
    Vector3 RaycastTerrain(Vector2 mousePos);
}