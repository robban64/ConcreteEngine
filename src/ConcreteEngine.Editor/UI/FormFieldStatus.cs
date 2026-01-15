using System.Numerics;
using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal struct FormFieldStatus()
{
    private int _field = 0;
    private int _activeField = -1;
    private int _editedField = -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextFieldDrag()
    {
        var field = _field;
        (int activeField, int deactivatedField) = GuiUtils.ItemActivatedAndDeactivatedAfterEdit(_field++);
        _activeField &= activeField;
        _editedField &= deactivatedField;
        return field;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextField()
    {
        var deactivatedField = ImGui.IsItemDeactivatedAfterEdit();
        _activeField &= -1;
        _editedField &= deactivatedField ? _field : -1;
        return _field++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasEdited(out int field)
    {
        field = _editedField;
        if (_activeField == -1 && _editedField >= 0) return true;
        field = -1;
        return false;
    }
}

internal static class FormFieldStatusExtensions
{
    extension(ref FormFieldStatus field)
    {
        public void InputFloat(ReadOnlySpan<byte> prop, string id, ref float v)
        {
            ImGui.TextUnformatted(prop);
            ImGui.Separator();
            ImGui.InputFloat(id, ref v);
            field.NextField();
        }

        public void InputFloat2(ReadOnlySpan<byte> prop, string id, ref Vector2 v)
        {
            ImGui.TextUnformatted(prop);
            ImGui.Separator();
            ImGui.InputFloat2(id, ref v);
            field.NextField();
        }
    
        public void InputFloat3(ReadOnlySpan<byte> prop, string id, ref Vector3 v)
        {
            ImGui.TextUnformatted(prop);
            ImGui.Separator();
            ImGui.InputFloat3(id, ref v);
            field.NextField();
        }
    
        public void ColorEdit4(ReadOnlySpan<byte> prop, string id, ref Vector4 v)
        {
            ImGui.TextUnformatted(prop);
            ImGui.Separator();
            ImGui.ColorEdit4(id, ref v);
            field.NextFieldDrag();
        }
    }
}