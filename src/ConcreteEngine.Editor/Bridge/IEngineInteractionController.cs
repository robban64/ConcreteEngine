using System.Numerics;
using ConcreteEngine.Editor.Store;

namespace ConcreteEngine.Editor.Bridge;

public interface IEngineInteractionController
{
    EditorId Raycast(Vector2 mousePos);
    Vector3 RaycastEntityOnTerrain(EditorId entity, Vector2 mousePos, Vector3 origin);
    Vector3 RaycastTerrain(Vector2 mousePos);
}