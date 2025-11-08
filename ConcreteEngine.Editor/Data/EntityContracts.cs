using ConcreteEngine.Shared.TransformData;

namespace ConcreteEngine.Editor.Data;

public readonly struct EntityDataPayload(int entityId, in EditorEntityModel model, in TransformData transform)
{
    public readonly int EntityId = entityId;
    public readonly EditorEntityModel Model = model;
    public readonly TransformData Transform = transform;
}

public readonly struct EntityTransformPayload(int entityId, in TransformData transform)
{
    public readonly int EntityId = entityId;
    public readonly TransformData Transform = transform;
}