using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;

#pragma warning disable CS8524

namespace ConcreteEngine.Editor.UI;

internal enum InputComponents : byte
{
    Float1, Float2, Float3
}

internal struct FormFieldInputs(float width, bool vertical)
{
    public const float VerticalWidth = 210f;

    public static FormFieldInputs MakeVertical() => new(VerticalWidth, true);

    public FormFieldInputs() : this(0, false) { }

    public float Width = width;
    private short _field = 0;
    private short _activeField = -1;
    private short _editedField = -1;
    public bool Vertical = vertical;

    
    public void ToggleVertical()
    {
        Width = VerticalWidth;
        Vertical = true;
    }

    public void ToggleDefault()
    {
        Width = 0;
        Vertical = false;
    }

    public bool HasEdited(out int field)
    {
        field = _editedField;
        if (_activeField == -1 && _editedField >= 0) return true;
        field = -1;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int NextFieldDrag()
    {
        ImGui.PopID();

        var activeField = ImGui.IsItemActive() ? _field : (short)-1;
        var deactivatedField = ImGui.IsItemDeactivatedAfterEdit() ? _field : (short)-1;
        _field++;

        _activeField &= activeField;
        _editedField &= deactivatedField;
        return _field;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int NextField()
    {
        ImGui.PopID();

        var deactivatedField = ImGui.IsItemDeactivatedAfterEdit();
        _activeField &= -1;
        _editedField &= (short)(deactivatedField ? _field : -1);
        return _field++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NewField(ReadOnlySpan<byte> label, out ReadOnlySpan<byte> inputLabel)
    {
        ImGui.PushID(_field);

        if (Vertical)
        {
            ImGui.TextUnformatted(label);
            ImGui.Separator();
            inputLabel = "##input"u8;
        }
        else
        {
            inputLabel = label;
        }

        if (Width > 0) ImGui.SetNextItemWidth(Width);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NewField(ref byte label, out ReadOnlySpan<byte> inputLabel)
    {
        ImGui.PushID(_field);

        if (Vertical)
        {
            ImGui.TextUnformatted(ref label);
            ImGui.Separator();
            inputLabel = "##input"u8;
        }
        else
        {
            inputLabel = new ReadOnlySpan<byte>(in label);
        }

        if (Width > 0) ImGui.SetNextItemWidth(Width);
    }


    public bool InputFloat(ReadOnlySpan<byte> label, InputComponents comp, ref float v, string? format = null)
    {
        NewField(label, out var inputLabel);
        var res = comp switch
        {
            InputComponents.Float1 => ImGui.InputFloat(inputLabel, ref v, format),
            InputComponents.Float2 => ImGui.InputFloat2(inputLabel, ref v, format),
            InputComponents.Float3 => ImGui.InputFloat3(inputLabel, ref v, format),
        };
        NextField();
        return res;
    }

    public bool SliderFloat(ReadOnlySpan<byte> label, InputComponents comp, ref float v, float min, float max,
        string format = "")
    {
        NewField(label, out var inputLabel);
        var res = comp switch
        {
            InputComponents.Float1 => ImGui.SliderFloat(inputLabel, ref v, min, max, format),
            InputComponents.Float2 => ImGui.SliderFloat2(inputLabel, ref v, min, max, format),
            InputComponents.Float3 => ImGui.SliderFloat3(inputLabel, ref v, min, max, format),
        };
        NextFieldDrag();
        return res;
    }
    
    public bool InputFloat(ref byte label, InputComponents comp, ref float v, string? format = null)
    {
        NewField(ref label, out var inputLabel);
        var res = comp switch
        {
            InputComponents.Float1 => ImGui.InputFloat(inputLabel, ref v, format),
            InputComponents.Float2 => ImGui.InputFloat2(inputLabel, ref v, format),
            InputComponents.Float3 => ImGui.InputFloat3(inputLabel, ref v, format),
        };
        NextField();
        return res;
    }

    public bool SliderFloat(ref byte label, InputComponents comp, ref float v, float min, float max,
        string format = "")
    {
        NewField(ref label, out var inputLabel);
        var res = comp switch
        {
            InputComponents.Float1 => ImGui.SliderFloat(inputLabel, ref v, min, max, format),
            InputComponents.Float2 => ImGui.SliderFloat2(inputLabel, ref v, min, max, format),
            InputComponents.Float3 => ImGui.SliderFloat3(inputLabel, ref v, min, max, format),
        };
        NextFieldDrag();
        return res;
    }
    public bool DragFloat(ref byte label, InputComponents comp, ref float f0, float speed, float min,
        float max, string format = "")
    {
        NewField(ref label, out var inputLabel);
        var res = comp switch
        {
            InputComponents.Float1 => ImGui.DragFloat(inputLabel, ref f0, speed, min, max, format),
            InputComponents.Float2 => ImGui.DragFloat2(inputLabel, ref f0, speed, min, max, format),
            InputComponents.Float3 => ImGui.DragFloat3(inputLabel, ref f0, speed, min, max, format),
        };
        NextFieldDrag();
        return res;
    }

    public bool DragFloat(ReadOnlySpan<byte> label, InputComponents comp, ref float f0, float speed, float min,
        float max, string format = "")
    {
        NewField(label, out var inputLabel);
        var res = comp switch
        {
            InputComponents.Float1 => ImGui.DragFloat(inputLabel, ref f0, speed, min, max, format),
            InputComponents.Float2 => ImGui.DragFloat2(inputLabel, ref f0, speed, min, max, format),
            InputComponents.Float3 => ImGui.DragFloat3(inputLabel, ref f0, speed, min, max, format),
        };
        NextFieldDrag();
        return res;
    }

    public bool ColorEdit3(ReadOnlySpan<byte> label, ref float f0)
    {
        NewField(label, out var inputLabel);
        var res = ImGui.ColorEdit3(inputLabel, ref f0);
        NextFieldDrag();
        return res;
    }

    public bool ColorEdit4(ReadOnlySpan<byte> label, ref float f0)
    {
        NewField(label, out var inputLabel);
        var res = ImGui.ColorEdit4(inputLabel, ref f0);
        NextFieldDrag();
        return res;
    }
}