using ConcreteEngine.Shared.TransformData;

namespace ConcreteEngine.Editor.Data;

public struct EntityDataPayload(int entityId, in EditorEntityModel model, in TransformData transform)
{
    public int EntityId = entityId;
    public EditorEntityModel Model = model;
    public TransformData Transform = transform;
}

public readonly struct EntityTransformPayload(int entityId, in TransformData transform)
{
    public readonly int EntityId = entityId;
    public readonly TransformData Transform = transform;
}