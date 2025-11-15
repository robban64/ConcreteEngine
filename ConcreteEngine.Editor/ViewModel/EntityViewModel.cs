#region

using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;

#endregion

namespace ConcreteEngine.Editor.ViewModel;

public sealed class EntitiesViewModel
{
    public List<EntityRecord> Entities { get; set; } = [];

    private EntityDataPayload _data;
    private EntityDataState _state;

    public ref readonly EntityDataPayload Data => ref _data;
    internal ref EntityDataState DataState => ref _state;


    public void FillData(EntityRecord entity, in ApiDataRefRequest<EntityDataPayload> api)
    {
        _data.EntityId = entity.EntityId;
        api.FillData(entity.EntityId, ref _data);

        _state.ModelId = _data.Model.ModelId;
        _state.MaterialTagKey = _data.Model.MaterialTagKey;
        _state.Transform.From(in _data.Transform);
    }

    public void FillData(in ApiDataRefRequest<EntityDataPayload> api)
    {
        var idx = SortMethod.BinarySearchInt(Entities, _data.EntityId);
        InvalidOpThrower.ThrowIf(idx < 0, nameof(_data.EntityId));
        var entity = Entities[idx];
        FillData(entity, in api);
    }


    public void WriteData(EntityRecord record, in ApiDataRefRequest<EntityDataPayload> api)
    {
        record.Generation++;
        UpdateDataFromState(record);
        api.WriteData(record.Generation, ref _data);
    }

    private void UpdateDataFromState(EntityRecord record)
    {
        _state.Transform.Fill(out var transform);
        _data.EntityId = record.EntityId;
        _data.Transform = transform;
    }
}

public sealed class EntityRecord(
    int entityId,
    string name,
    int componentCount) : IComparable<EntityRecord>, IComparable<int>
{
    public int EntityId { get; } = entityId;
    public string Name { get; } = name;
    public int ComponentCount { get; } = componentCount;
    public long Generation { get; internal set; } = 0;

    public int CompareTo(EntityRecord? other) => other is null ? 1 : EntityId.CompareTo(other.EntityId);
    public int CompareTo(int other) => EntityId.CompareTo(other);
}