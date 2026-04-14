using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Core;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;

internal sealed unsafe class ComboField : PropertyField<Int1Value>
{
    private const int NameCapacity = 32;

    public ushort StartAt { get; set; } = 0;
    private short _index = -1;
    private int _lastValue = int.MinValue;

    private byte* _nameStr;

    private readonly byte[][] _names;
    private readonly int[] _values;

    public string Placeholder { get; private set; }

    protected override int SizeInBytes => sizeof(int) + NameCapacity;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ComboField(
        string name,
        ReadOnlySpan<int> values,
        ReadOnlySpan<string> names,
        Func<Int1Value>? getter = null,
        Action<Int1Value>? setter = null
    ) : base(name, sizeof(int) + NameCapacity, getter, setter)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(values.Length, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, names.Length);
        Delay = FieldGetDelay.VeryHigh;

        _values = new int[values.Length];
        values.CopyTo(_values.AsSpan());

        _names = names.ToUtf8ByteArrays();
        _nameStr = Allocator.AllocSlice(NameCapacity);

    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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

    public void SetItemName(int index, string newName) => _names[index] = newName.ToUtf8();

    public ComboField WithPlaceholder(string placeholder)
    {
        ArgumentNullException.ThrowIfNull(placeholder);
        Placeholder = placeholder;
        return this;
    }

    public ComboField WithStartAt(int startAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startAt);
        StartAt = (ushort)startAt;
        return this;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void UpdateValue()
    {
        var value = Value->X;
        _index = (short)_values.AsSpan().IndexOf(value);
        _lastValue = value;

        var sw = new UnsafeSpanWriter(_nameStr, NameCapacity);
        if ((uint)_index < (uint)_names.Length && _index >= StartAt)
            sw.Write(_names[_index]);
        else
            sw.Write(Placeholder);
    }

    protected override bool OnDraw()
    {
        ref var value = ref Get().X;
        if (_lastValue != value)
            UpdateValue();

        if (!ImGui.BeginCombo(GetLabel(), _nameStr)) return false;

        var changed = false;
        var writer = TextBuffers.GetWriter();
        var length = _names.Length;
        for (var i = StartAt; i < length; i++)
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
        return changed;
    }
}
