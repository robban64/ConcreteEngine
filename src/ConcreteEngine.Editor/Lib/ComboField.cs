using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

internal static class ComboCache
{
    private static readonly Dictionary<string, CacheEntry> _entries = new(16);

    public static void Add(string key, int[] values, string[] names)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, names.Length);
        _entries.Add(key, new CacheEntry(values, names));
    }

    public static bool TryGet(string key, out int[] values, out string[] names)
    {
        if (!_entries.TryGetValue(key, out var entry))
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

internal sealed class ComboField : InputValueField<int>
{
    private static unsafe UnsafeSpanWriter _writer = new(EditorBuffers.TextBuffer, EditorBuffers.TextBuffer.Capacity);

    private readonly string[] _names;
    private readonly int[] _values;

    private String16Utf8 _placeholder = new (EmptyPlaceholder);

    private int _index;
    private int _lastValue;
    /*    public int StartAt
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
    */

    public ComboField(string name, int[] values, string[] names,
        Func<int>? getter, Action<int>? setter) : base(name, getter, setter)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(values.Length, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, names.Length);

        _values = values;
        _names = names;

        Delay = PropertyGetDelay.VeryHigh;
    }

    public static ComboField MakeFromCache(string key, string name, Func<int>? getter, Action<int>? setter) 
    {
        if (!ComboCache.TryGet(key, out var cacheValues, out var cacheNames))
            throw new KeyNotFoundException(key);
        
        return new ComboField(name, cacheValues, cacheNames, getter, setter);
    }


    public static ComboField MakeFromEnumCache<T>(string name, Func<int>? getter,
        Action<int>? setter) where T : unmanaged, Enum
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

    public ComboField WithPlaceholder(string placeholder)
    {
        _placeholder = new String16Utf8(placeholder);
        return this;
    }
    
    private ref byte GetPreview()
    {
        return ref (uint)_index < (uint)_names.Length
            ? ref _writer.Append(_names[_index]).End()
            : ref _placeholder.GetRef();
    }

    protected override bool Draw(ref byte label, ref int value, ref byte format)
    {
        if (_lastValue != value)
            _index = _values.IndexOf(value);

        _lastValue = value;

        var changed = false;

        if (ImGui.BeginCombo(ref label, ref GetPreview()))
        {
            for (var i = 0; i < _names.Length; i++)
            {
                var isSelected = i == _index;
                if (ImGui.Selectable(ref _writer.Append(_names[i]).End(), isSelected))
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