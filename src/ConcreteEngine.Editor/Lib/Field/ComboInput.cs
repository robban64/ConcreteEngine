using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;

internal sealed unsafe class ComboInput : InputField
{
    private int _lastValue = int.MinValue;
    private int _index = -1;

    private readonly NativeString _displayText;

    private readonly int[] _values;
    private readonly string[] _names;

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
        Action<int> setter) : base(label, InputKind.Combo)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(values.Length, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, names.Length);

        _values = new int[values.Length];
        values.CopyTo(_values.AsSpan());
        _names = names.ToArray();

        _setter = setter;

        LabelPlacement = LabelPlacement.None;
        _displayText = StringArena.AllocateString(32);
    }

    public int Value { get; set; }

    public void SetItemName(int index, string newName) => _names[index] = newName;

    public bool Draw()
    {
        if (_lastValue != Value) OnChanged();

        DrawLabel();
        var open = ImGui.BeginCombo(StringId, _displayText);
        if (open && DrawInner() && ShouldTrigger())
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
        _displayText.Set(name);
    }

    private bool DrawInner()
    {
        var sw = ScratchBuffer.Writer();
        var changed = false;
        var length = _names.Length;
        for (var i = StartAt; i < length; i++)
        {
            var isSelected = i == _index;
            sw.Append(_names[i]);
            sw.Append(StringId);
            if (ImGui.Selectable(sw.Append((byte)'-').Append(i).End(), isSelected))
            {
                _index = i;
                Value = _values[i];
                changed = true;
            }

            if (isSelected) ImGui.SetItemDefaultFocus();
        }

        ImGui.EndCombo();
        return changed;
    }

    public static ComboInput Create(string label,
        ReadOnlySpan<int> values,
        ReadOnlySpan<string> names,
        Action<int> setter,
        int startAt = 0,
        string placeHolder = "")
    {
        return new ComboInput(label, values, names, setter) { StartAt = startAt, Placeholder = placeHolder };
    }

    public static ComboInput Create(string label,
        ReadOnlySpan<byte> values,
        ReadOnlySpan<string> names,
        Action<int> setter,
        int startAt = 0,
        string placeHolder = "")
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(values.Length, 64);
        Span<int> ints = stackalloc int[values.Length];
        for (int i = 0; i < values.Length; i++) ints[i] = values[i];
        return new ComboInput(label, ints, names, setter) { StartAt = startAt, Placeholder = placeHolder };
    }

    public static ComboInput Create(string label,
        ReadOnlySpan<ushort> values,
        ReadOnlySpan<string> names,
        Action<int> setter,
        int startAt = 0,
        string placeHolder = "")
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(values.Length, 64);
        Span<int> ints = stackalloc int[values.Length];
        for (int i = 0; i < values.Length; i++) ints[i] = values[i];
        return new ComboInput(label, ints, names, setter) { StartAt = startAt, Placeholder = placeHolder };
    }
}