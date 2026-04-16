using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Core;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;

internal sealed unsafe class ComboField : PropertyField
{
    private const int PreviewCapacity = 32;

    public readonly PropertyFieldBinding<Int1Value> Binding;
    public string Placeholder { get; private set; }

    public ushort StartAt { get; set; } = 0;
    private short _index = -1;
    private int _lastValue = int.MinValue;

    private readonly byte[][] _names;
    private readonly int[] _values;


    protected override int CustomDataSize => PreviewCapacity;
    

    public ComboField(
        string name,
        ReadOnlySpan<int> values,
        ReadOnlySpan<string> names,
        Func<Int1Value>? getter = null,
        Action<Int1Value>? setter = null
    ) : base(name)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(values.Length, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, names.Length);

        Binding = new PropertyFieldBinding<Int1Value>();
        if(getter != null && setter != null)
            Binding.Bind(getter,setter);


        Delay = FieldGetDelay.VeryHigh;

        _values = new int[values.Length];
        values.CopyTo(_values.AsSpan());

        _names = names.ToUtf8ByteArrays();

    }
    
    public override IPropertyFieldBinding GetBinding() => Binding;
    public override void Refresh() => Binding.Refresh(Memory.GetValue<Int1Value>());
    protected override void Set() => Binding.Set(Memory.GetValue<Int1Value>());

    public void SetItemName(int index, string newName) => _names[index] = newName.ToUtf8();
    
    public void Bind(Func<Int1Value> getter , Action<Int1Value> setter) => Binding.Bind(getter, setter);

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
    private void SyncValue(int value)
    {
        _index = (short)_values.AsSpan().IndexOf(value);
        _lastValue = value;

        var sw = Memory.CustomData.Writer();
        if ((uint)_index < (uint)_names.Length && _index >= StartAt)
            sw.Write(_names[_index]);
        else
            sw.Write(Placeholder);
    }

    protected override bool OnDraw()
    {
        var value = (int*)Binding.Get(Memory.GetValue<Int1Value>());
        if (_lastValue != *value)
            SyncValue(*value);

        if (!ImGui.BeginCombo(GetLabel(), Memory.CustomData)) return false;

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
                *value = _values[i];
                changed = true;
            }

            if (isSelected) ImGui.SetItemDefaultFocus();
            ImGui.PopID();
        }

        ImGui.EndCombo();
        return changed;
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
}
