using ImGuiNET;

namespace ConcreteEngine.Editor.Utils;

public struct ImGuiFieldStatus()
{
    public int Field = 0;
    public int ActiveField = -1;
    public int EditedField = -1;

    public int NextFieldDrag()
    {
        var field = Field;
        (int activeField, int deactivatedField) = GuiUtils.ItemActivatedAndDeactivatedAfterEdit(Field++);
        ActiveField &= activeField;
        EditedField &= deactivatedField;
        return field;
    }

    public int NextField()
    {
        var deactivatedField = ImGui.IsItemDeactivatedAfterEdit();
        ActiveField &= -1;
        EditedField &= deactivatedField ? Field : -1;
        return Field++;
    }

    public bool HasEdited(out int field)
    {
        field = EditedField;
        if (ActiveField == -1 && EditedField >= 0) return true;

        field = -1;
        return false;
    }
}