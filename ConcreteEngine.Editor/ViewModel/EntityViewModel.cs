using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Shared.TransformData;

namespace ConcreteEngine.Editor.ViewModel;

public sealed class EntitiesViewModel
{
    public List<EntityRecord> Entities { get; set; } = [];

    private EntityDataPayload _data;
    private EntityDataState _state;

    public ref readonly EntityDataPayload Data => ref _data;
    internal ref EntityDataState DataState => ref _state;
    
    public void UpdateDataFrom(EntityRecord record, GenericDataRequest<EntityDataPayload> requestDel)
    {
        _data.EntityId = record.EntityId;
        requestDel(ref _data);
        _state = new EntityDataState(in _data);
    }

    public void UpdateData(GenericDataRequest<EntityDataPayload> requestDel)
    {
        requestDel(ref _data);

        _state.ModelId = _data.Model.ModelId;
        _state.MaterialTagKey = _data.Model.MaterialTagKey;
        _state.Transform.From(in _data.Transform);
    }


    public void FillTransform(out EntityTransformPayload payload)
    {
        _state.Transform.Fill(in _data.Transform.Rotation, out var data);
        payload = new EntityTransformPayload(_state.EntityId, in data);
    }
}

public sealed class EntityRecord(
    int entityId,
    string name,
    int componentCount)
{
    public int EntityId { get; } = entityId;
    public string Name { get; } = name;
    public int ComponentCount { get; } = componentCount;
}