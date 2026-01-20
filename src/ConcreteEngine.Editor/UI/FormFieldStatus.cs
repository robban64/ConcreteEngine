using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal struct FormFieldStatus(bool useTopLabel = true)
{
    private int _field = 0;
    private int _activeField = -1;
    private int _editedField = -1;

    public bool UseTopLabel = useTopLabel;

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<byte> DrawLabelOrGetId(ReadOnlySpan<byte> prop, string id)
        {
            var sw = Widgets.GetWriter2();
            if (prop.IsEmpty) return sw.Write(id);
            if (!field.UseTopLabel) return sw.Start(prop).Append(id).End();

            ImGui.TextUnformatted(prop);
            ImGui.Separator();
            return sw.Write(id);
        }

        public bool InputFloat(ReadOnlySpan<byte> prop, string id, ref float v, string? format = null)
        {
            var label = field.DrawLabelOrGetId(prop, id);
            var res = ImGui.InputFloat(label, ref v, format);
            field.NextField();
            return res;
        }

        public bool SliderFloat(ReadOnlySpan<byte> prop, string id, ref float v, float min, float max,
            string format = "")
        {
            var label = field.DrawLabelOrGetId(prop, id);
            var res = ImGui.SliderFloat(label, ref v, min, max);
            field.NextFieldDrag();
            return res;
        }


        public bool InputFloat2(ReadOnlySpan<byte> prop, string id, ref Vector2 v, string format = "")
        {
            var label = field.DrawLabelOrGetId(prop, id);
            var res = ImGui.InputFloat2(label, ref v.X);
            field.NextField();
            return res;
        }

        public bool InputFloat3(ReadOnlySpan<byte> prop, string id, ref Vector3 v, string format = "")
        {
            var label = field.DrawLabelOrGetId(prop, id);
            var res = ImGui.InputFloat3(label, ref v.X);
            field.NextField();
            return res;
        }

        public bool ColorEdit4(ReadOnlySpan<byte> prop, string id, ref Vector4 v)
        {
            var label = field.DrawLabelOrGetId(prop, id);
            var res = ImGui.ColorEdit4(label, ref v.X);
            field.NextFieldDrag();
            return res;
        }

        public bool ColorEdit4(ReadOnlySpan<byte> prop, string id, ref Color4 v)
        {
            var label = field.DrawLabelOrGetId(prop, id);
            var res = ImGui.ColorEdit4(label, ref Unsafe.As<Color4, Vector4>(ref v).X);
            field.NextFieldDrag();
            return res;
        }
    }
}