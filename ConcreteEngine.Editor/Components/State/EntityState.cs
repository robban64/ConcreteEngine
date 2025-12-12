#region

using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;

#endregion

namespace ConcreteEngine.Editor.Components.State;

internal sealed class EntityViewState
{
}


/*
    public EditorEntityResource? GetSelectedEntity()
    {
        var editorId = EditorDataStore.State.SelectedEntity;
        if (!editorId.IsValid || editorId.ItemType != EditorItemType.Entity) return null;
        if (_selectedEntity?.Id != editorId)
            _selectedEntity = FindEntity(editorId);
        return _selectedEntity;
    }


    public static EditorEntityResource? FindEntity(int entityId)
    {
        if (!EditorManagedStore.TryGet<EditorEntityResource>((entityId, EditorItemType.Entity), out var entity))
            return null;

        return entity;
    }
 */