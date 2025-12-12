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
    private int _rotationField = -1;
    private int _editedField = -1;

    private int _prevEditField = -1;

    public void SetSelectedEntity(EditorId entityId)
    {
        if (EditorDataStore.SelectedEntity == entityId) return;

        if(entityId.IsValid)
            EngineController.SelectEntity(entityId);
        else
            EngineController.DeSelectEntity();
        
    }

    public void UpdateTransform(int field, int rotationField)
    {
        var entityId = EditorDataStore.SelectedEntity;
        if(!entityId.IsValid || field < 0) return;

        _prevEditField = _editedField;
        _editedField = field;
        _rotationField = rotationField;
        
        if (rotationField != -1)
            EditorDataStore.EntityState.Transform.ApplyRotationFromEuler();
        
        EngineController.CommitEntity();

    }

    public void BeforeDraw()
    {
        if (_prevEditField == -1 && _editedField > -1)
        {
            _prevEditField = _editedField;
            return;
        }

        _editedField = -1;
        _rotationField = -1;
    }
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