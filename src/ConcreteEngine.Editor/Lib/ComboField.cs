using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

internal sealed class ComboField : InputValueField<int>
{
    private readonly String16Utf8[] _names;
    private readonly int[] _values;

    private int _index;
    private int _lastValue;

    public int StartAt
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(field);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(field, _values.Length);
            if (_index < field) _index = field;
            field = value;
        }
    }


    public ComboField(string name, string placeholder, int[] values, ReadOnlySpan<string> names,
        Func<int>? getter, Action<int>? setter) : base(name, getter, setter)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(values.Length, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, names.Length);

        _values = values;
        _names = new String16Utf8[names.Length];

        for (var i = 0; i < names.Length; i++)
            _names[i] = new String16Utf8(names[i]);

        if (!string.IsNullOrEmpty(placeholder))
            _names[0] = new String16Utf8(placeholder);

        Delay = PropertyGetDelay.VeryHigh;
    }

    public static ComboField MakeFromSpan(string name, string placeholder,
        ReadOnlySpan<int> values, ReadOnlySpan<string> names, Func<int>? getter, Action<int>? setter)
    {
        var newValues = new int[values.Length];
        values.CopyTo(newValues);
        return new ComboField(name, placeholder, newValues, names, getter, setter);
    }


    public static ComboField MakeFromEnumCache<T>(string name, string placeholder, Func<int>? getter,
        Action<int>? setter)
        where T : unmanaged, Enum
    {
        var names = EnumCache<T>.Names;
        var values = EnumCache<T>.Values.AsSpan();
        var enumSize = Unsafe.SizeOf<T>();
        var intValues = new int[values.Length];
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

        return new ComboField(name, placeholder, intValues, names, getter, setter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref byte GetPreview()
    {
        return  ref (uint)_index < (uint)_names.Length
            ? ref _names[_index].GetRef()
            : ref MemoryMarshal.GetReference(EmptyPlaceholder);

    }

    protected override bool Draw(ref byte label, ref int value, ref byte format)
    {
        if (_lastValue != value)
            _index = _values.IndexOf(value);

        _lastValue = value;

        var changed = false;

        if (ImGui.BeginCombo(ref label, ref GetPreview()))
        {
            for (var i = StartAt; i < _names.Length; i++)
            {
                var isSelected = i == _index;
                if (ImGui.Selectable(ref _names[i].GetRef(), isSelected))
                {
                    _index = i;
                    value = _values[i];
                    changed = true;
                }

                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }

        return changed;
    }
}