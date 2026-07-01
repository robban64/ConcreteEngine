using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Lib.Field;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;

internal sealed unsafe class ComboInput : UiField
{
    public Int1 Value;

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


    public ComboInput(string label, ReadOnlySpan<int> values, ReadOnlySpan<string> names)
        : base(label, FieldWidgetKind.Combo)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(values.Length, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, names.Length);

        _values = new int[values.Length];
        values.CopyTo(_values.AsSpan());
        _names = names.ToArray();
        Layout = FieldLayout.None;
    }

    public override ref byte GetRawValue() => ref Unsafe.As<Int1, byte>(ref Value);

    public void SetItemName(int index, string newName) => _names[index] = newName;

    [SkipLocalsInit]
    public override bool Draw()
    {
        var value = (int)Value;
        if (_lastValue != value)
            OnChanged(value);

        var buffer = stackalloc byte[LabelAllocCapacity * 2];
        var label = ApplyLabelLayout(buffer);
        var sw = new NativeSpanWriter(buffer + LabelAllocCapacity, LabelAllocCapacity);

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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ComboInput MakeFromEnumCache<T>(string label) where T : unmanaged, Enum
    {
        var names = EnumCache<T>.Names;
        var values = EnumCache<T>.Values.AsSpan();
        var enumSize = Unsafe.SizeOf<T>();
        Span<int> intValues = stackalloc int[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            intValues[i] = enumSize switch
            {
                1 => Unsafe.As<T, byte>(ref values[i]),
                2 => Unsafe.As<T, short>(ref values[i]),
                4 => Unsafe.As<T, int>(ref values[i]),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        return new ComboInput(label, intValues, names);
    }
}