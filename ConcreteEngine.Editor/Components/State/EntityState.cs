#region

using ConcreteEngine.Editor.Components.Data;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;

#endregion

namespace ConcreteEngine.Editor.Components.State;

internal sealed class EntitiesViewModel
{
    public EditorEntityResource? SelectedEntity { get; private set; }

    public EditorEntityResource? FindEntity(int entityId)
    {
        if (!EditorManagedStore.TryGet<EditorEntityResource>((entityId, EditorItemType.Entity), out var entity))
            return null;

        return entity;
    }

    public void MakeDirty()
    {
        EditorDataStore.Input.EntitySelection.IsDirty = true;
    }

    public void SetSelectedEntity(EditorId entityId)
    {
        if (SelectedEntity?.Id == entityId) return;

        if (entityId > 0 &&
            EditorManagedStore.TryGet<EditorEntityResource>((entityId, EditorItemType.Entity), out var entity))
        {
            SelectedEntity = entity;
            EditorDataStore.Input.EntitySelection.EntityId = entityId;
            EditorDataStore.Input.EntitySelection.IsRequesting = true;
            return;
        }

        SelectedEntity = null;
        EditorDataStore.Input.EntitySelection.EntityId = EditorId.Empty;

        //EditorDataStore.Slot<EntityDataState>.State.Reset(0);
    }
}