using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

public sealed class ComboField : InputValueField<int>
{
    private readonly String16Utf8 _placeholder;
    private readonly String16Utf8[] _names;
    private readonly int[] _values;

    private int _index;
    private int _lastValue;

    public ComboField(string name, string placeholder, int[] values, ReadOnlySpan<string> names,
        Func<int>? getter, Action<int>? setter) : base(name, getter, setter)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(values.Length, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, names.Length);

        _values = values;
        _names = new String16Utf8[names.Length];
        _placeholder = new String16Utf8(placeholder);

        for (var i = 0; i < names.Length; i++)
            _names[i] = new String16Utf8(names[i]);

        if (!string.IsNullOrEmpty(placeholder))
            _names[0] = new String16Utf8(placeholder);
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

    protected override bool Draw(ref byte label, ref int value, ref byte format)
    {
        if (_lastValue != value)
            _index = _values.IndexOf(value);

        _lastValue = value;

        var names = _names;
        var len = names.Length;

        ref var preview = ref (uint)_index < (uint)len
            ? ref names[_index].GetRef()
            : ref EmptyPlaceholder.GetRef();

        if (!ImGui.BeginCombo(ref label, ref preview)) return false;

        var changed = false;

        for (var i = 0; i < len; i++)
        {
            var isSelected = i == _index;
            if (ImGui.Selectable(ref names[i].GetRef(), isSelected))
            {
                _index = i;
                value = _values[i];
                changed = true;
            }

            if (isSelected)
                ImGui.SetItemDefaultFocus();
        }

        ImGui.EndCombo();

        return changed;
    }
}