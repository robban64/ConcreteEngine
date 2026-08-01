using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;

internal sealed unsafe class InputGroup : InputField
{
    private int _count;
    private FrameStepper _stepper = new(8);

    private readonly InputEntry[] _inputs;
    private readonly NativeView<InputNumeric1> _values;

    private readonly Action<Span<InputNumeric1>> _getter;
    private readonly Action<Span<InputNumeric1>> _setter;


    public InputGroup(string label, int count, Action<Span<InputNumeric1>> getter, Action<Span<InputNumeric1>> setter)
        : base(label, InputKind.Group)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 16);

        _values = StringArena.Instance.AllocRaw(sizeof(int) * count).Reinterpret<InputNumeric1>();
        _inputs = new InputEntry[count];
        _getter = getter;
        _setter = setter;
    }

    public bool Draw()
    {
        if (_count != _inputs.Length) Throwers.InvalidOperation(nameof(_count));
        if (_stepper.Tick()) _getter(_values.AsSpan());

        ImGui.PushID(StringId);

        var changed = false;
        var value = _values.Ptr;
        var inputs = new ReadOnlySpan<InputEntry>(_inputs);
        for (var i = 0; i < inputs.Length; ++i, ++value)
            changed |= inputs[i].Draw(value);

        ImGui.PopID();
        if (changed && ShouldTrigger())
        {
            _setter(_values.AsSpan());
            return true;
        }

        return false;
    }

    public InputGroup WithFloatInput(string name, InputStyle style, float speed, float min, float max,
        string fmt = "%.2f") =>
        Add(new InputEntry(CreateNativeLabel(name, _count, out var start), start, style, true, speed, min, max, fmt));

    public InputGroup WithIntInput(string name, InputStyle style, float speed, int min, int max) =>
        Add(new InputEntry(CreateNativeLabel(name, _count, out var start), start, style, false, speed, min, max));

    private InputGroup Add(InputEntry entry)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(_count, _inputs.Length);
        _inputs[_count++] = entry;
        return this;
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
        public bool Draw(InputNumeric1* value)
        {
            DrawLabel(_label, LabelPlacement.Inline);
            return _isFloat ? DrawFloat(value) : DrawInt(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DrawFloat(InputNumeric1* value)
        {
            return _style switch
            {
                InputStyle.Input => InputNumeric1.DrawFloatInput(StringId, value, _format),
                InputStyle.Slider => InputNumeric1.DrawFloatSlider(StringId, value, _format, _min, _max),
                InputStyle.Drag => InputNumeric1.DrawFloatDrag(StringId, value, _format, _speed, _min, _max),
                _ => false
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DrawInt(InputNumeric1* value)
        {
            return _style switch
            {
                InputStyle.Input => InputNumeric1.DrawIntInput(StringId, value),
                InputStyle.Slider => InputNumeric1.DrawIntSlider(StringId, value, (int)_min, (int)_max),
                InputStyle.Drag => InputNumeric1.DrawIntDrag(StringId, value, _speed, (int)_min, (int)_max),
                _ => false
            };
        }
    }
}