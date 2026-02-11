using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

public sealed class ComboField : InputValueField<int>
{
    private readonly String16Utf8[] _names;
    private readonly int[] _values;

    private int _index;
    private int _lastValue;

    public ComboField(string name, ReadOnlySpan<int> values, ReadOnlySpan<string> names, Func<int> getter,
        Action<int> setter) : base(name, getter, setter)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(values.Length, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, names.Length);

        _names = new String16Utf8[names.Length];
        _values = new int[values.Length];

        values.CopyTo(_values);
        for (int i = 0; i < names.Length; i++)
            _names[i] = new String16Utf8(names[i]);
    }

    public static ComboField MakeFromEnumCache<T>(string name, Func<int> getter, Action<int> setter)
        where T : unmanaged, Enum
    {
        var names = EnumCache<T>.Names;
        var values = MemoryMarshal.Cast<T, int>(EnumCache<T>.Values);
        return new ComboField(name, values, names, getter, setter);
    }

    protected override bool OnDraw(ref byte label, ref int value, ref byte format)
    {
        if (_lastValue != value)
            _index = _values.IndexOf(value);

        _lastValue = value;

        var changed = false;

        var names = _names;
        var len = names.Length;

        ref var preview = ref (uint)_index < len
            ? ref names[_index].GetRef()
            : ref EmptyPlaceholder.GetRef();

        if (ImGui.BeginCombo(ref label, ref preview))
        {
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
        }

        ImGui.EndCombo();

        return changed;
    }
}