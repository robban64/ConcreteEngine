#region

using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Editor.Data;

public struct EntityDataPayload(int entityId, in EditorEntityModel model, in TransformData transform)
{
    public int EntityId = entityId;
    public EditorEntityModel Model = model;
    public TransformData Transform = transform;
}
