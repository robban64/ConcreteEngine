#region

using ConcreteEngine.Common.Collections;
using ConcreteEngine.Editor.Components.Data;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;

#endregion

namespace ConcreteEngine.Editor.Components.State;

internal sealed class EntitiesViewModel
{
    public EditorEntityResource? SelectedEntity { get; private set; }

    private long _generation;

    private EntityDataState _data;
    private EntityDataState _state;

    public ref EntityDataState Data => ref _data;
    internal ref EntityDataState DataState => ref _state;

    public long Generation => _generation;


    public EditorEntityResource? FindEntity(int entityId)
    {
        if (!EditorManagedStore.TryGet<EditorEntityResource>((entityId, EditorItemType.Entity), out var entity))
            return null;
        
        return entity;
    }

    public void SetSelectedEntity(EditorId entityId)
    {
        if (entityId == 0)
        {
            SelectedEntity = null;
            RefreshData();
            return;
        }

        if (SelectedEntity?.Id == entityId) return;

        SelectedEntity = FindEntity(entityId);
        RefreshData();
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
        _data.EntityId = SelectedEntity.Id;
    }
}
