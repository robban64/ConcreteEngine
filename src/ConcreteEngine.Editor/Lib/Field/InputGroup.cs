using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;

internal sealed unsafe class InputGroup : InputField
{
    private readonly InputEntry[] _inputs;
   // private readonly CompositeValue[] _values;
    private readonly NativeView<int> _values;

    private int _count;

    private readonly Action<Span<CompositeValue>> _getter;
    private readonly Action<Span<CompositeValue>> _setter;

    private FrameStepper _stepper = new(4);
    
    public InputGroup(string label, int count, Action<Span<CompositeValue>> getter, Action<Span<CompositeValue>> setter)
        : base(label, InputKind.Group)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 16);

        _values = StringArena.Instance.AllocRaw(sizeof(int) * count).Reinterpret<int>();
        _inputs = new InputEntry[count];
        //_values = new CompositeValue[count];
        _getter = getter;
        _setter = setter;
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<CompositeValue> GetValueSpan() => MemoryMarshal.Cast<int, CompositeValue>(_values.AsSpan());

    public bool Draw()
    {
        if(_count != _inputs.Length) Throwers.InvalidOperation(nameof(_count));
        if (_stepper.Tick()) _getter(GetValueSpan());

        ImGui.PushID(StringId);
        
        var changed = false;
        var value = (CompositeValue*)_values.Ptr;
        var inputs = new ReadOnlySpan<InputEntry>(_inputs);
        for (var i = 0; i < inputs.Length; ++i, ++value)
            changed |= inputs[i].Draw(value);

        ImGui.PopID();
        if (changed && ShouldTrigger())
        {
            _setter(GetValueSpan());
            return true;
        }

        return false;
    }

    public InputGroup WithFloatInput(string name, InputStyle style, float speed, float min, float max,
        string fmt = "%.2f") =>
        Add(new InputEntry(WriteLabel(name, _count, out var start), start, style, true, speed, min, max, fmt));

    public InputGroup WithIntInput(string name, InputStyle style, float speed, int min, int max) =>
        Add(new InputEntry(WriteLabel(name, _count, out var start), start, style, false, speed, min, max));

    private InputGroup Add(InputEntry entry)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(_count, _inputs.Length);
        _inputs[_count++] = entry;
        return this;
    }


    [StructLayout(LayoutKind.Explicit)]
    internal struct CompositeValue
    {
        [FieldOffset(0)] public int IntValue;
        [FieldOffset(0)] public float FloatValue;

        public CompositeValue(int value) => IntValue = value;
        public CompositeValue(float value) => FloatValue = value;
        
        public static implicit operator CompositeValue(int v) => new(v);
        public static implicit operator CompositeValue(float v) => new(v);
        public static explicit operator int(CompositeValue v) => v.IntValue;
        public static explicit operator float(CompositeValue v) => v.FloatValue;
    }

    private readonly struct InputEntry(
        NativeString label,
        byte idStart,
        InputStyle style,
        bool isFloat,
        float speed,
        float min,
        float max,
        String8Utf8 format = default)
    {
        private readonly NativeString _label = label;
        private readonly String8Utf8 _format = format;
        private readonly float _speed = speed, _min = min, _max = max;
        private readonly InputStyle _style = style;
        private readonly byte _idStart = idStart;
        private readonly bool _isFloat = isFloat;
        
        private byte* StringId => _label.TextStart + _idStart;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Draw(CompositeValue* value)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(_label.TextStart);
            ImGui.SameLine(GuiTheme.FormItemInlineOffset);
            ImGui.SetNextItemWidth(GuiTheme.FormItemInlineWidth);
            return _isFloat ? DrawFloat(&value->FloatValue) : DrawInt(&value->IntValue);
        }

        private bool DrawFloat(float* value)
        {
            var fmt = _format;
            return _style switch
            {
                InputStyle.Input => Float1.DrawInput(StringId, value, (byte*)&fmt),
                InputStyle.Slider => Float1.DrawSlider(StringId, value, (byte*)&fmt, _min, _max),
                InputStyle.Drag => Float1.DrawDrag(StringId, value, (byte*)&fmt, _speed, _min, _max),
                _ => false
            };
        }

        private bool DrawInt(int* value)
        {
            return _style switch
            {
                InputStyle.Input => Int1.DrawInput(StringId, value),
                InputStyle.Slider => Int1.DrawSlider(StringId, value, (int)_min, (int)_max),
                InputStyle.Drag => Int1.DrawDrag(StringId, value, _speed, (int)_min, (int)_max),
                _ => false
            };
        }
    }
}