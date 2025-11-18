#region

using ConcreteEngine.Editor.Data;

#endregion

namespace ConcreteEngine.Editor.DataState;

internal struct EntityDataState
{
    public int ModelId;
    public int MaterialTagKey;
    public TransformDataState Transform;

    public EntityDataState(in EntityDataPayload payload)
    {
        ModelId = payload.Model.ModelId;
        MaterialTagKey = payload.Model.MaterialTagKey;
        Transform.From(in payload.Transform);
    }
}