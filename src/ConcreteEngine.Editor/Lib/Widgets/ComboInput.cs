using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Lib.Field;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;

internal sealed unsafe class ComboInput : NumberInput<Int1Value>
{
    private int _index = -1;

    private int _lastValue = int.MinValue;

    private readonly byte[][] _names;
    private readonly int[] _values;

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
            if (Placeholder.Length == 0) field = "None";
            else if (value.Length >= LabelAllocCapacity) field = value[..LabelAllocCapacity];
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
        _names = names.ToUtf8ByteArrays();
        Layout = FieldLayout.None;
    }

    public void WithPlaceholder(string placeholder)
    {
        Placeholder = placeholder;
    }


    [SkipLocalsInit]
    public override bool Draw()
    {
        var value = (int)Value;
        if (_lastValue != value)
        {
            _index = _values.AsSpan().IndexOf(value);
            _lastValue = value;
        }

        var buffer = stackalloc byte[LabelAllocCapacity * 2];

        var label = DrawWriteLabel(buffer);
        var sw = new UnsafeSpanWriter(buffer + LabelAllocCapacity, LabelAllocCapacity);

        var preview = (uint)_index < (uint)_names.Length && _index >= StartAt
            ? sw.Write(_names[_index])
            : sw.Write(Placeholder);

        return ImGui.BeginCombo(label, preview) && DrawInner(sw);
    }

    private bool DrawInner(UnsafeSpanWriter sw)
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