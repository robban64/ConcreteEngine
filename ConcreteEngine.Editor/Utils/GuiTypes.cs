namespace ConcreteEngine.Editor.Utils;

public struct ImGuiDragStatus()
{
    public int Field = 0;
    public int ActiveField = -1;
    public int DeactivatedField = -1;

    public void NextField()
    {
        (int activeField, int deactivatedField) = GuiUtils.ItemActivatedAndDeactivatedAfterEdit(Field++);
        ActiveField &= activeField;
        DeactivatedField &= deactivatedField;
    }

    public bool HasEdited() => ActiveField == -1 || DeactivatedField >= 0;
}