using ConcreteEngine.Editor.Data;
using ConcreteEngine.Shared.TransformData;

namespace ConcreteEngine.Editor.DataState;

internal struct EntityDataState
{
    public readonly int EntityId;
    public int ModelId;
    public int MaterialTagKey;
    public TransformDataState Transform;
    public readonly TransformData BaseTransform;

    public EntityDataState( in EntityDataPayload payload)
    {
        EntityId = payload.EntityId;
        ModelId = payload.Model.ModelId;
        MaterialTagKey = payload.Model.MaterialTagKey;
        BaseTransform = payload.Transform;
        Transform.FromStable(in payload.Transform);
    }
}