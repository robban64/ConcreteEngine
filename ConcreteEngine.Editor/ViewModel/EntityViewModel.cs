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
    
    public EntityRecord? SelectedEntity { get; private set; }

    private EntityDataPayload _data;
    private EntityDataState _state;

    public ref EntityDataPayload Data => ref _data;
    internal ref EntityDataState DataState => ref _state;


    public EntityRecord? FindEntity(int entityId)
    {
        if (Entities.Count == 0) return null;
        
        if(entityId < Entities.Count && Entities[entityId].EntityId == entityId) 
            return Entities[entityId];
        
        var index = SortMethod.BinarySearchInt(Entities, entityId);
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
        
        if(SelectedEntity?.EntityId == entityId) return;
        
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