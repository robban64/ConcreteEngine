using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal struct ImGuiFieldStatus()
{
    private int _field = 0;
    private int _activeField = -1;
    private int _editedField = -1;

    public int NextFieldDrag()
    {
        var field = _field;
        (int activeField, int deactivatedField) = GuiUtils.ItemActivatedAndDeactivatedAfterEdit(_field++);
        _activeField &= activeField;
        _editedField &= deactivatedField;
        return field;
    }

    public int NextField()
    {
        var deactivatedField = ImGui.IsItemDeactivatedAfterEdit();
        _activeField &= -1;
        _editedField &= deactivatedField ? _field : -1;
        return _field++;
    }

    public bool HasEdited(out int field)
    {
        field = _editedField;
        if (_activeField == -1 && _editedField >= 0) return true;

        field = -1;
        return false;
    }
}