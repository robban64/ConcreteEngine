#region

using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;

#endregion

namespace ConcreteEngine.Editor.Components.State;

internal sealed class EntitiesViewModel
{
    private EditorEntityResource? _selectedEntity = null;
    
    public bool HasSelectedEntity => _selectedEntity != null;

    public EditorEntityResource? GetSelectedEntity()
    {
        var editorId = EditorDataStore.State.SelectedId;
        if (!editorId.IsValid || editorId.ItemType != EditorItemType.Entity) return null;
        if (_selectedEntity?.Id != editorId)
            _selectedEntity = FindEntity(editorId);
        return _selectedEntity;
    }

    public EditorEntityResource? FindEntity(int entityId)
    {
        if (!EditorManagedStore.TryGet<EditorEntityResource>((entityId, EditorItemType.Entity), out var entity))
            return null;

        return entity;
    }

    public void MakeDirty()
    {
        EditorDataStore.Input.EditorSelection.IsDirty = true;
    }

    public void SetSelectedEntity(EditorId entityId)
    {
        if (EditorDataStore.State.SelectedId == entityId) return;

        ref var selection = ref EditorDataStore.Input.EditorSelection;
        if (entityId > 0)
        {
            selection.Id = entityId;
            selection.IsRequesting = true;
            
            StateContext.SetRightSidebarState(RightSidebarMode.Property);
            return;
        }

        selection.Id = EditorId.Empty;
        selection.IsDirty = true;
    }
}