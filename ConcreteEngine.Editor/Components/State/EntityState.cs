#region

using ConcreteEngine.Common.Numerics.Maths;
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
        if (EditorDataStore.Input.EditorSelection.Id == entityId) return;

        ref var selection = ref EditorDataStore.Input.EditorSelection;
        selection.Id = entityId.IsValid ? entityId : EditorId.Empty;
        selection.IsRequesting = true;
    }

    public void UpdateTransform(int field, int rotationField)
    {
        _prevEditField = _editedField;
        _editedField = field;
        _rotationField = rotationField;
    }

    public void Refresh()
    {
        if (_editedField < 0) return;
        
        ref var transform = ref EditorDataStore.State.EntityState.Transform;
        if (_rotationField != -1)
            transform.Rotation = RotationMath.EulerDegreesToQuaternion(in transform.EulerAngles);

        EditorDataStore.Input.EditorSelection.IsDirty = true;
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