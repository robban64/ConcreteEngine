#region

using ConcreteEngine.Common.Collections;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Editor.Utils;

#endregion

namespace ConcreteEngine.Editor.ViewModel;

public sealed class EntitiesViewModel
{
    public List<EntityRecord> Entities { get; set; } = [];

    public EntityRecord? SelectedEntity { get; private set; }

    private EntityDataPayload _data;
    private EntityDataState _state;

    public ref EntityDataPayload Data => ref _data;
    internal ref EntityDataState DataState => ref _state;


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


    public void FillData(in ApiDataRefRequest<EntityDataPayload> api)
    {
        api.FillData(_data.EntityId, ref _data);
        _state.ModelId = _data.Model.ModelId;
        _state.MaterialTagKey = _data.Model.MaterialTagKey;
        _state.Transform.From(in _data.Transform);
    }

    public void WriteData(in ApiDataRefRequest<EntityDataPayload> api)
    {
        RefreshData();
        var currentGen = SelectedEntity?.Generation ?? 0;
        var responseGen = api.WriteData(currentGen, ref _data);
    }

    private void RefreshData()
    {
        if (SelectedEntity is null)
        {
            _state = default;
            _data = default;
            return;
        }

        _state.Transform.Fill(out var transform);
        _data.EntityId = SelectedEntity.EntityId;
        _data.Transform = transform;
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