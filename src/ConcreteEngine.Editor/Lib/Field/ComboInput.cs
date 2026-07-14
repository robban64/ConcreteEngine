using System.Text;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Core.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;

internal sealed unsafe class ComboInput : InputField
{
    public int Value;
    private int _lastValue = int.MinValue;

    private int _index = -1;

    private readonly byte[] _displayText = new byte[32];

    private readonly int[] _values;
    private readonly string[] _names;

    private readonly Func<int> _getter;
    private readonly Action<int> _setter;

    public int StartAt
    {
        get;
        set => field = int.Min((ushort)value, _values.Length - 1);
    }

    public string Placeholder
    {
        get;
        set => field = string.IsNullOrEmpty(value) ? "None" : value;
    } = "None";

    public ComboInput(string label,
        ReadOnlySpan<int> values,
        ReadOnlySpan<string> names,
        Func<int> getter,
        Action<int> setter) : base(label, InputKind.Combo)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(values.Length, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, names.Length);

        _values = new int[values.Length];
        values.CopyTo(_values.AsSpan());
        _names = names.ToArray();

        _getter = getter;
        _setter = setter;

        LabelPlacement = LabelPlacement.None;
    }

    public void SetItemName(int index, string newName) => _names[index] = newName;

    public bool Draw()
    {
        Value = _getter();
        if (_lastValue != Value) OnChanged();

        var sw = ScratchBuffer.Writer();
        var label = ApplyLabelLayout(sw);
        sw.SetCursor(label.Length + 1);
        var changed = ImGui.BeginCombo(label, sw.Write(_displayText)) && DrawInner(sw);
        if (changed)
        {
            _setter(Value);
            return true;
        }

        return false;
    }

    private void OnChanged()
    {
        _index = _values.IndexOf(Value);
        _lastValue = Value;

        var name = (uint)_index < (uint)_names.Length && _index >= StartAt ? _names[_index] : Placeholder;

        int written = Encoding.UTF8.GetBytes(name.Truncate(31), _displayText);
        _displayText[int.Min(written, 31)] = 0;
    }

    private bool DrawInner(NativeSpanWriter sw)
    {
        var changed = false;
        var length = _names.Length;
        for (var i = StartAt; i < length; i++)
        {
            ImGui.PushID(i);
            var isSelected = i == _index;
            if (ImGui.Selectable(sw.Write(_names[i]), isSelected))
            {
                _index = i;
                Value = _values[i];
                changed = true;
            }

            if (isSelected) ImGui.SetItemDefaultFocus();
            ImGui.PopID();
        }

        ImGui.EndCombo();
        return changed;
    }

    public static ComboInput Create(string label,
        ReadOnlySpan<int> values,
        ReadOnlySpan<string> names,
        Func<int> getter,
        Action<int> setter,
        int startAt = 0,
        string placeHolder = "")
    {
        return new ComboInput(label, values, names, getter, setter) { StartAt = startAt, Placeholder = placeHolder };
    }

    public static ComboInput Create(string label,
        ReadOnlySpan<byte> values,
        ReadOnlySpan<string> names,
        Func<int> getter,
        Action<int> setter,
        int startAt = 0,
        string placeHolder = "")
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(values.Length, 64);
        Span<int> ints = stackalloc int[values.Length];
        for (int i = 0; i < values.Length; i++) ints[i] = values[i];
        return new ComboInput(label, ints, names, getter, setter) { StartAt = startAt, Placeholder = placeHolder };
    }

    public static ComboInput Create(string label,
        ReadOnlySpan<ushort> values,
        ReadOnlySpan<string> names,
        Func<int> getter,
        Action<int> setter,
        int startAt = 0,
        string placeHolder = "")
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(values.Length, 64);
        Span<int> ints = stackalloc int[values.Length];
        for (int i = 0; i < values.Length; i++) ints[i] = values[i];
        return new ComboInput(label, ints, names, getter, setter) { StartAt = startAt, Placeholder = placeHolder };
    }
}