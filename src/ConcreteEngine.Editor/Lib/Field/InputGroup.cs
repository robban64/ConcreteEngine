using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;

internal sealed unsafe class InputGroup : InputField
{
    private ComponentEntry[] _inputs = new ComponentEntry[4];
    private CompositeValue[] _values = new CompositeValue[4];

    private byte _components;

    private readonly Action<Span<CompositeValue>> _getter;
    private readonly Action<Span<CompositeValue>> _setter;

    private FrameStepper _stepper = new(4);

    public InputGroup(string label, Action<Span<CompositeValue>> getter, Action<Span<CompositeValue>> setter)
        : base(label, InputKind.Float)
    {
        _getter = getter;
        _setter = setter;
    }

    public bool Draw()
    {
        var values = _values.AsSpan(0, _components);
        if (_stepper.Tick()) _getter(values);

        var changed = false;
        var inputs = _inputs.AsSpan(0, _components);

        ImGui.PushID(StringId);
        for (var i = 0; i < inputs.Length; ++i)
        {
            var value = values[i];
            if (inputs[i].Draw(&value))
            {
                values[i] = value;
                changed = true;
            }
        }

        ImGui.PopID();

        if (changed && ShouldTrigger())
        {
            _setter(values);
            return true;
        }

        return false;
    }

    public void AddFloatInput(string name, InputStyle style, float speed, float min, float max, string fmt = "%.2f") =>
        Add(new ComponentEntry(WriteLabel(name, _components, out var start), start, style, true, speed, min, max, fmt));

    public void AddIntInput(string name, InputStyle style, float speed, int min, int max) =>
        Add(new ComponentEntry(WriteLabel(name, _components, out var start), start, style, false, speed, min, max));

    private InputGroup Add(ComponentEntry entry)
    {
        ArgumentNullException.ThrowIfNull(_inputs);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(_components, 4);
        if (_components >= _inputs.Length)
        {
            var newSize = _inputs.Length + 4;
            Array.Resize(ref _inputs, newSize);
            Array.Resize(ref _values, newSize);
        }

        _inputs[_components] = entry;
        _components++;
        return this;
    }


    [StructLayout(LayoutKind.Explicit)]
    internal struct CompositeValue
    {
        [FieldOffset(0)] public int IntValue;
        [FieldOffset(0)] public float FloatValue;

        public CompositeValue(int value) => IntValue = value;
        public CompositeValue(float value) => FloatValue = value;
    }

    private readonly struct ComponentEntry(
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Draw(CompositeValue* value)
        {
            return _isFloat ? DrawFloat(&value->FloatValue) : DrawInt(&value->IntValue);
        }

        private bool DrawFloat(float* value)
        {
            var fmt = _format;
            return _style switch
            {
                InputStyle.Input => Float1.DrawInput(_label, value, (byte*)&fmt),
                InputStyle.Slider => Float1.DrawSlider(_label, value, (byte*)&fmt, _min, _max),
                InputStyle.Drag => Float1.DrawDrag(_label, value, (byte*)&fmt, _speed, _min, _max),
                _ => false
            };
        }

        private bool DrawInt(int* value)
        {
            return _style switch
            {
                InputStyle.Input => Int1.DrawInput(_label, value),
                InputStyle.Slider => Int1.DrawSlider(_label, value, (int)_min, (int)_max),
                InputStyle.Drag => Int1.DrawDrag(_label, value, _speed, (int)_min, (int)_max),
                _ => false
            };
        }
    }
}