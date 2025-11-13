namespace ConcreteEngine.Editor.Utils;

public struct ImGuiFieldStatus()
{
    public int Field = 0;
    public int ActiveField = -1;
    public int DeactivatedField = -1;

    public int NextField()
    {
        var field = Field;
        (int activeField, int deactivatedField) = GuiUtils.ItemActivatedAndDeactivatedAfterEdit(Field++);
        ActiveField &= activeField;
        DeactivatedField &= deactivatedField;
        return field;
    }

    public bool HasEdited(out int field)
    {
        field = DeactivatedField;
        if (ActiveField == -1 || DeactivatedField >= 0) return true;

        field = -1;
        return false;
    }
}