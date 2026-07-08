using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Lib.Inspection;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;

internal sealed unsafe class ComboInput : InputField
{
    public int Value;

    private int _index = -1;
    private int _lastValue = int.MinValue;

    private readonly byte[] _displayText = new byte[32];

    private readonly int[] _values;
    private readonly string[] _names;

    public ushort StartAt
    {
        get;
        set => field = (ushort)int.Min(value, _values.Length - 1);
    }

    public string Placeholder
    {
        get;
        set
        {
            if (value.Length == 0) field = "None";
            else if (value.Length >= 32) field = value.Truncate(31).ToString();
            else field = value;
        }
    } = "None";


    public ComboInput(string label, ReadOnlySpan<int> values, ReadOnlySpan<string> names) : base(label, InputKind.Combo)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(values.Length, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, names.Length);

        _values = new int[values.Length];
        values.CopyTo(_values.AsSpan());
        _names = names.ToArray();
        LabelPlacement = LabelPlacement.None;
    }

    public void SetItemName(int index, string newName) => _names[index] = newName;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ref byte GetRawValue() => ref Unsafe.As<int, byte>(ref Value);
    
    public override bool Draw()
    {
        var value = (int)Value;
        if (_lastValue != value)
            OnChanged(value);

        var sw = TextBuffers.GetWriter();
        var label = ApplyLabelLayout(sw);
        sw.SetCursor(label.Length + 1);
        return ImGui.BeginCombo(label, sw.Write(_displayText)) && DrawInner(sw);
    }

    private void OnChanged(int value)
    {
        _index = _values.IndexOf(value);
        _lastValue = value;

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

    public static ComboInput Create(string label, ReadOnlySpan<byte> values, ReadOnlySpan<string> names)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(values.Length, 64);
        Span<int> ints = stackalloc int[values.Length];
        for(int i = 0; i < values.Length; i++) ints[i] = values[i];
        return new ComboInput(label, ints, names);
    }
    
    public static ComboInput Create(string label, ReadOnlySpan<ushort> values, ReadOnlySpan<string> names)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(values.Length, 64);
        Span<int> ints = stackalloc int[values.Length];
        for(int i = 0; i < values.Length; i++) ints[i] = values[i];
        return new ComboInput(label, ints, names);
    }

}