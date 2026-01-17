using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
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
        int activeField = ImGui.IsItemActive() ? field : -1;
        int deactivatedField = ImGui.IsItemDeactivatedAfterEdit() ? field : -1;
        _field++;

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
        public bool InputFloat(ReadOnlySpan<byte> prop, string id, ref float v, string? format = null)
        {
            ImGui.TextUnformatted(prop);
            ImGui.Separator();
            var res = ImGui.InputFloat(id, ref v, format);
            field.NextField();
            return res;
        }

        public bool SliderFloat(ReadOnlySpan<byte> prop, string id, ref float v, float min, float max,
            string format = "")
        {
            ImGui.TextUnformatted(prop);
            ImGui.Separator();
            var res = ImGui.SliderFloat(id, ref v, min, max);
            field.NextFieldDrag();
            return res;
        }


        public bool InputFloat2(ReadOnlySpan<byte> prop, string id, ref Vector2 v, string format = "")
        {
            ImGui.TextUnformatted(prop);
            ImGui.Separator();
            var res = ImGui.InputFloat2(id, ref v);
            field.NextField();
            return res;
        }

        public bool InputFloat3(ReadOnlySpan<byte> prop, string id, ref Vector3 v, string format = "")
        {
            ImGui.TextUnformatted(prop);
            ImGui.Separator();
            var res = ImGui.InputFloat3(id, ref v);
            field.NextField();
            return res;
        }

        public bool ColorEdit4(ReadOnlySpan<byte> prop, string id, ref Vector4 v)
        {
            ImGui.TextUnformatted(prop);
            ImGui.Separator();
            var res = ImGui.ColorEdit4(id, ref v);
            field.NextFieldDrag();
            return res;
        }

        public bool ColorEdit4(ReadOnlySpan<byte> prop, string id, ref Color4 v)
        {
            ImGui.TextUnformatted(prop);
            ImGui.Separator();
            var res = ImGui.ColorEdit4(id, ref Unsafe.As<Color4, Vector4>(ref v));
            field.NextFieldDrag();
            return res;
        }
    }
}