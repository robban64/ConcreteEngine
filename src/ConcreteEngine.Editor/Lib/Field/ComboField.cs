using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Core;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;

internal sealed unsafe class ComboField : PropertyField<Int1Value>
{
    private NativeView<String16Utf8> _names;
    private NativeView<int> _values;
    private String16Utf8* _placeholder;

    private int _lastValue = int.MinValue;

    private short _index = -1;
    public ushort StartAt { get; set; } = 0;

    protected override int SizeInBytes => (sizeof(int) * _values.Length) + (16 * _names.Length) + 16;

    public ComboField(
        string name,
        ReadOnlySpan<int> values,
        ReadOnlySpan<string> names,
        Func<Int1Value>? getter = null,
        Action<Int1Value>? setter = null
    ) : base(name, (sizeof(int) * values.Length) + (16 * names.Length) + 16 + sizeof(int), getter, setter)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(values.Length, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, names.Length);
        Delay = FieldGetDelay.VeryHigh;

        _placeholder = Allocator.AllocSlice<String16Utf8>();
        _names = Allocator.AllocSlice<String16Utf8>(names.Length);
        _values = Allocator.AllocSlice<int>(values.Length);
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
        ArgumentNullException.ThrowIfNull(placeholder);
        _placeholder[0] = new String16Utf8(placeholder);
        return this;
    }

    public ComboField WithStartAt(int startAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startAt);
        StartAt = (ushort)startAt;
        return this;
    }

    protected override bool OnDraw()
    {
        ref var value = ref Get().GetRef();
        if (_lastValue != value)
        {
            _index = (short)_values.AsSpan().IndexOf(value);
            _lastValue = value;
        }

        var preview = (uint)_index < (uint)_names.Length && _index >= StartAt
            ? _names + _index
            : _placeholder;

        var changed = false;
        if (ImGui.BeginCombo(GetLabel(), (byte*)preview))
        {
            for (var i = StartAt; i < _names.Length; i++)
            {
                ImGui.PushID(i);
                var isSelected = i == _index;
                if (ImGui.Selectable((byte*)(_names + i), isSelected))
                {
                    _index = (short)i;
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



internal sealed unsafe class ComboField2 : PropertyField<Int1Value>
{
    private const int nameCapacity = 32;

    public ushort StartAt { get; set; } = 0;
    private short _index = -1;
    private int _lastValue = int.MinValue;

    private byte* _nameStr;

    private readonly byte[][] _names;
    private readonly int[] _values;

    public string Placeholder;

    protected override int SizeInBytes => sizeof(int) + nameCapacity;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ComboField2(
        string name,
        ReadOnlySpan<int> values,
        ReadOnlySpan<string> names,
        Func<Int1Value>? getter = null,
        Action<Int1Value>? setter = null
    ) : base(name, sizeof(int) + nameCapacity, getter, setter)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(values.Length, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, names.Length);
        Delay = FieldGetDelay.VeryHigh;

        _values = new int[values.Length];
        values.CopyTo(_values.AsSpan());

        _names = names.ToUtf8ByteArrays();
        _nameStr = Allocator.AllocSlice(nameCapacity);

    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ComboField2 MakeFromEnumCache<T>(string name, Func<Int1Value>? getter = null,
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

        return new ComboField2(name, intValues, names, getter, setter);
    }

    public void SetItemName(int index, string newName) => _names[index] = newName.ToUtf8();

    public ComboField2 WithPlaceholder(string placeholder)
    {
        ArgumentNullException.ThrowIfNull(placeholder);
        Placeholder = placeholder;
        return this;
    }

    public ComboField2 WithStartAt(int startAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startAt);
        StartAt = (ushort)startAt;
        return this;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void UpdateValue(scoped ref int value)
    {
        _index = (short)_values.AsSpan().IndexOf(value);
        _lastValue = value;

        var sw = new UnsafeSpanWriter(_nameStr, nameCapacity);
        if ((uint)_index < (uint)_names.Length && _index >= StartAt)
            sw.Write(_names[_index]);
        else
            sw.Write(Placeholder);
    }

    protected override bool OnDraw()
    {
        ref var value = ref Get().GetRef();
        if (_lastValue != value)
            UpdateValue(ref value);

        var changed = false;
        if (ImGui.BeginCombo(GetLabel(), _nameStr))
        {
            var writer = TextBuffers.GetWriter();
            for (var i = StartAt; i < _names.Length; i++)
            {
                ImGui.PushID(i);
                var isSelected = i == _index;
                if (ImGui.Selectable(writer.Write(_names[i]), isSelected))
                {
                    _index = (short)i;
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
