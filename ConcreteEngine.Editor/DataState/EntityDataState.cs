using ConcreteEngine.Editor.Data;
using ConcreteEngine.Shared.TransformData;

namespace ConcreteEngine.Editor.DataState;

internal struct EntityDataState
{
    public readonly int EntityId;
    public int ModelId;
    public int MaterialTagKey;
    public TransformDataState Transform;

    public EntityDataState(in EntityDataPayload payload)
    {
        EntityId = payload.EntityId;
        ModelId = payload.Model.ModelId;
        MaterialTagKey = payload.Model.MaterialTagKey;
        Transform.From(in payload.Transform);
    }
}