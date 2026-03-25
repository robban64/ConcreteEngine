using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

internal sealed unsafe class ComboField : PropertyField<Int1Value>
{
    private NativeViewPtr<String16Utf8> _names;
    private NativeViewPtr<int> _values;
    private String16Utf8* _placeholder;

    private int _index = -1;
    private int _lastValue = int.MinValue;
    public int StartAt { get; set; } = 0;

    protected override int SizeInBytes => (sizeof(int) * _values.Length) + (16 * _names.Length) + 16;

    public ComboField(
        string name,
        ReadOnlySpan<int> values,
        ReadOnlySpan<string> names,
        Func<Int1Value>? getter = null,
        Action<Int1Value>? setter = null
    ) : base(name, ((sizeof(int) * values.Length) + (16 * names.Length) + 16 + sizeof(int)), getter, setter)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(values.Length, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, names.Length);
        Delay = FieldGetDelay.VeryHigh;

        _placeholder = Allocator->AllocSlice<String16Utf8>();
        _names = Allocator->AllocSlice<String16Utf8>(names.Length);
        _values = Allocator->AllocSlice<int>(values.Length);
        values.CopyTo(_values.AsSpan());

        for (int i = 0; i < names.Length; i++)
        {
            _names[i] = new String16Utf8(names[i]);
        }
    }

    public static ComboField MakeFromEnumCache<T>(string name, Func<Int1Value>? getter = null,
        Action<Int1Value>? setter = null) where T : unmanaged, Enum
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

        return new ComboField(name, intValues, names, getter, setter);
    }

    public void SetItemName(int index, string newName) => _names[index] = newName;

    public ComboField WithPlaceholder(string placeholder)
    {
        _placeholder[0] = new String16Utf8(placeholder);
        return this;
    }

    public ComboField WithStartAt(int startAt)
    {
        StartAt = startAt;
        return this;
    }

    protected override bool OnDraw()
    {
        ref var value = ref Get().GetRef();
        if (_lastValue != value)
        {
            _index = _values.AsSpan().IndexOf(value);
            _lastValue = value;
        }

        var preview = (uint)_index < (uint)_names.Length && _index >= StartAt
            ? _names + _index
            : _placeholder;

        var changed = false;
        if (ImGui.BeginCombo(ref GetLabel(), ref preview->GetRef()))
        {
            for (var i = StartAt; i < _names.Length; i++)
            {
                ImGui.PushID(i);
                var isSelected = i == _index;
                if (ImGui.Selectable((byte*)(_names + i), isSelected))
                {
                    _index = i;
                    value = _values[i];
                    changed = true;
                }

                if (isSelected) ImGui.SetItemDefaultFocus();
                ImGui.PopID();
            }

            ImGui.EndCombo();
        }

        return changed;
    }
}

/*
internal static class ComboCache
{
    private static readonly Dictionary<string, CacheEntry> Entries = new(16);

    public static void Add(string key, int[] values, String16Utf8[] names)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, names.Length);
        Entries.Add(key, new CacheEntry(values, names));
    }

    public static bool TryGet(string key, out int[] values, out String16Utf8[] names)
    {
        if (!Entries.TryGetValue(key, out var entry))
        {
            values = [];
            names = [];
            return false;
        }

        values = entry.Values;
        names = entry.Names;
        return true;
    }

    private class CacheEntry(int[] values, String16Utf8[] names)
    {
        public readonly int[] Values = values;
        public readonly String16Utf8[] Names = names;
    }
}


internal static class ComboCache2
{
    private static readonly Dictionary<string, CacheEntry> Entries = new(16);

    public static void Add(string key, int[] values, NativeViewPtr<byte> ptr)
    {
        Entries.Add(key, new CacheEntry(values, ptr));
    }

    public static bool TryGet(string key, out int[] values, out NativeViewPtr<byte> ptr)
    {
        if (!Entries.TryGetValue(key, out var entry))
        {
            values = [];
            ptr = default;
            return false;
        }

        values = entry.Values;
        ptr = entry.Ptr;
        return true;
    }

    private class CacheEntry(int[] values, NativeViewPtr<byte> ptr)
    {
        public readonly int[] Values = values;
        public readonly NativeViewPtr<byte> Ptr = ptr;
    }
}

internal unsafe sealed class ComboField : PropertyField<Int1Value>
{
    private const int NameStrLen = 16;

    private static NativeViewPtr<byte> AllocName(ReadOnlySpan<string> names)
    {
        var len = 0;
        foreach (var it in names) len += Encoding.UTF8.GetByteCount(it);

        len = IntMath.AlignUp(len, 4);
        var name = TextBuffers.WidgetArena.Alloc(len, true);

        var writer = name.Writer();
        foreach (var it in names)
            writer.Append(it).Append((char)0);

        writer.Append((char)0).End();
        return name;

    }

    private readonly NativeViewPtr<byte> _names;
    private readonly int[] _values;

    private int _index = -1;
    private int _lastValue = int.MinValue;
    public int StartAt { get; set; } = 0;


    public ComboField(string name, int[] values, NativeViewPtr<byte> names, Func<Int1Value> getter,
        Action<Int1Value> setter) : base(name, getter, setter)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(values.Length, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, names.Length);

        Delay = FieldGetDelay.VeryHigh;

        _names = names;
        _values = values;
    }

    public static ComboField Make(string name, ReadOnlySpan<int> values, ReadOnlySpan<string> names, Func<Int1Value> getter,
        Action<Int1Value> setter)
    {
        if (ComboCache2.TryGet(name, out var cacheValues, out var cacheNames))
            return new ComboField(name, cacheValues, cacheNames, getter, setter);

        var valueResult = values.ToArray();
        var nameResult = AllocName(names);

        ComboCache2.Add(name, valueResult, nameResult);
        return new ComboField(name, valueResult, nameResult, getter, setter);
    }


    public static ComboField MakeFromEnumCache<T>(string name, Func<Int1Value> getter,
        Action<Int1Value> setter) where T : unmanaged, Enum
    {
        var key = typeof(T).Name;
        if (ComboCache2.TryGet(name, out var cacheValues, out var cacheNames))
            return new ComboField(name, cacheValues, cacheNames, getter, setter);

        var names = AllocName(EnumCache<T>.Names);
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

        ComboCache2.Add(key, intValues, names);
        return new ComboField(name, intValues, names, getter, setter);
    }

    public void SetItemName(int index, string newName) => _names[index] = newName;

    public ComboField WithPlaceholder(string placeholder)
    {
        _placeholder = new String16Utf8(placeholder);
        return this;
    }

    public ComboField WithStartAt(int startAt)
    {
        StartAt = startAt;
        return this;
    }
    private unsafe bool Draw2(ref Int1Value value)
    {
        var currentValue = value.X;
        if (_lastValue != currentValue)
        {
            _index = _values.AsSpan().IndexOf(currentValue);
            _lastValue = currentValue;
        }

        int index = _index;
        bool changed = ImGui.Combo(Sw.Write(ref GetLabel()), &index, _names,);
        if (changed)
        {
            _index = index;
            value = _values[_index];
        }
        return changed;
    }
    protected override unsafe bool OnDraw(ref Int1Value value)
    {
        var currentValue = value.X;
        if (_lastValue != currentValue)
        {
            _index = _values.AsSpan().IndexOf(currentValue);
            _lastValue = currentValue;
        }

        var preview = (uint)_index < (uint)_names.Length && _index >= StartAt
            ? Sw.Write(ref _names[_index].GetRef())
            : Sw.Write(ref _placeholder.GetRef());

        var changed = false;
        if (ImGui.BeginCombo(Sw.Write(ref GetLabel(), 17), preview))
        {
            for (var i = StartAt; i < _names.Length; i++)
            {
                ImGui.PushID(i);
                var isSelected = i == _index;
                if (ImGui.Selectable(Sw.Write(ref _names[i].GetRef()), isSelected))
                {
                    _index = i;
                    value = _values[i];
                    changed = true;
                }

                if (isSelected) ImGui.SetItemDefaultFocus();
                ImGui.PopID();
            }

            ImGui.EndCombo();
        }

        return changed;
    }
}*/