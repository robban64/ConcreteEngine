using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

internal static class ComboCache
{
    private static readonly Dictionary<string, CacheEntry> Entries = new(16);

    public static void Add(string key, int[] values, string[] names)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, names.Length);
        Entries.Add(key, new CacheEntry(values, names));
    }

    public static bool TryGet(string key, out int[] values, out string[] names)
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

    private class CacheEntry(int[] values, string[] names)
    {
        public readonly int[] Values = values;
        public readonly string[] Names = names;
    }
}

internal sealed class ComboField : PropertyField<Int1Value>
{
    private static unsafe UnsafeSpanWriter _writer = new(EditorBuffers.TextBuffer, EditorBuffers.TextBuffer.Capacity);

    private readonly string[] _names;
    private readonly int[] _values;

    private String16Utf8 _placeholder = new(EmptyPlaceholder);

    private int _index = -1;
    private int _lastValue = int.MinValue;
    public int StartAt { get; set; } = 0;

    public ComboField(string name, int[] values, string[] names, Func<Int1Value> getter, Action<Int1Value> setter) :
        base(name, getter, setter)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(values.Length, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, names.Length);

        _values = values;
        _names = names;

        Delay = FieldGetDelay.VeryHigh;
    }

    public static ComboField MakeFromCache(string key, string name, Func<Int1Value> getter,
        Action<Int1Value> setter)
    {
        if (!ComboCache.TryGet(key, out var cacheValues, out var cacheNames))
            throw new KeyNotFoundException(key);

        return new ComboField(name, cacheValues, cacheNames, getter, setter);
    }


    public static ComboField MakeFromEnumCache<T>(string name, Func<Int1Value> getter,
        Action<Int1Value> setter) where T : unmanaged, Enum
    {
        var key = typeof(T).Name;
        if (ComboCache.TryGet(key, out var cacheValues, out var cacheNames))
            return new ComboField(name, cacheValues, cacheNames, getter, setter);

        var names = EnumCache<T>.Names.ToArray();
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

        ComboCache.Add(key, intValues, names);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref byte GetPreview()
    {
        return ref (uint)_index < (uint)_names.Length && _index >= StartAt
            ? ref _writer.Append(_names[_index]).End()
            : ref _placeholder.GetRef();
    }


    protected override bool OnDraw()
    {
        int currentValue = Get().X;
        if (_lastValue != currentValue)
        {
            _index = _values.AsSpan().IndexOf(currentValue);
            _lastValue = currentValue;
        }

        var changed = false;

        if (ImGui.BeginCombo(ref Name.GetRef(), ref GetPreview()))
        {
            for (var i = StartAt; i < _names.Length; i++)
            {
                ImGui.PushID(i);
                var isSelected = i == _index;
                if (ImGui.Selectable(ref _writer.Append(_names[i]).End(), isSelected))
                {
                    _index = i;
                    Value = _values[i];
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