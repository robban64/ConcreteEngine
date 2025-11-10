using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Shared.TransformData;

namespace ConcreteEngine.Editor.ViewModel;

public sealed class EntitiesViewModel
{
    public int SelectedEntityId { get; set; } = 0;
    public List<EntityRecord> Entities { get; set; } = [];

    private EntityDataPayload _data;
    private EntityDataState _state;
    
    public ref readonly EntityDataPayload Data => ref _data;
    internal ref EntityDataState DataState => ref _state;


    public void FromData(in EntityDataPayload payload)
    {
        _data = payload;
        _state = new EntityDataState(in payload);
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