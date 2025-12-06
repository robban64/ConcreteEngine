#region

using ConcreteEngine.Common.Collections;
using ConcreteEngine.Editor.Components.Data;
using ConcreteEngine.Editor.Data;

#endregion

namespace ConcreteEngine.Editor.Components.State;

internal sealed class EntitiesViewModel
{
    public List<EntityRecord> Entities { get; set; } = [];

    public EntityRecord? SelectedEntity { get; private set; }

    private long _generation;

    private EntityDataState _data;
    private EntityDataState _state;

    public ref EntityDataState Data => ref _data;
    internal ref EntityDataState DataState => ref _state;

    public long Generation => _generation;


    public EntityRecord? FindEntity(int entityId)
    {
        if (Entities.Count == 0 || entityId == 0) return null;

        if (entityId < Entities.Count && Entities[entityId].EntityId == entityId)
            return Entities[entityId];

        var index = SortMethod.BinarySearch(Entities, entityId);
        return index < 0 ? null : Entities[index];
    }

    public void SetSelectedEntity(int entityId)
    {
        if (entityId == 0)
        {
            SelectedEntity = null;
            RefreshData();
            return;
        }

        if (SelectedEntity?.EntityId == entityId) return;

        SelectedEntity = FindEntity(entityId);
        RefreshData();
    }


    public void FillView(ApiModelRequestDel<EntityRequestBody, List<EntityRecord>> api)
    {
        Entities = api(new EntityRequestBody(_data.EntityId)) ?? [];
    }

    public void Dispatch(ApiRefRequest<EntityDataState> api, bool isWriteRequest)
    {
        if (isWriteRequest)
        {
            var request = new EditorDataRequest<EntityDataState>(ref _generation, ref _state, isWriteRequest);
            api(ref request);
            if (request.HasNewData) _data = _state;
        }
        else
        {
            var request = new EditorDataRequest<EntityDataState>(ref _generation, ref _data, isWriteRequest);
            api(ref request);
            if (request.HasNewData) _state = _data;
        }
    }


    private void RefreshData()
    {
        if (SelectedEntity is null)
        {
            _state = default;
            _data = default;
            return;
        }

        _data = _state;
        _data.EntityId = SelectedEntity.EntityId;
    }
}

public sealed class EntityRecord : IComparable<EntityRecord>, IComparable<int>
{
    public int EntityId { get; }
    public string Name { get; }
    public int Model { get; }
    public int[] Materials { get; }
    public int ComponentCount { get; }
    public long Generation { get; internal set; } = 0;

    public string ModelText { get; private set; }
    public string MaterialText { get; private set; }

    public EntityRecord(int entityId,
        string name,
        int model,
        int[] materials,
        int componentCount)
    {
        EntityId = entityId;
        Model = model;
        Name = name;
        Materials = materials;
        ComponentCount = componentCount;

        ModelText = model.ToString();
        MaterialText = materials.Length == 0 ? string.Empty : string.Join(",", materials);
    }

    public int CompareTo(EntityRecord? other) => other is null ? 1 : EntityId.CompareTo(other.EntityId);
    public int CompareTo(int other) => EntityId.CompareTo(other);
}