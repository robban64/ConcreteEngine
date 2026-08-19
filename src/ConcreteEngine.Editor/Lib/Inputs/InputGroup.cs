using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Inputs;
/*
internal sealed unsafe class InputGroup : InputField
{
    private int _count;

    private readonly InputEntry[] _inputs;

    private readonly NativeView<InputNumeric1> _values;
    private readonly Action<Span<InputNumeric1>> _setter;

    public InputGroup(string label, int count, Action<Span<InputNumeric1>> setter)
        : base(label, InputKind.Group)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 16);

        _values = StringArena.Instance.AllocBytes(sizeof(int) * count).Reinterpret<InputNumeric1>();
        _inputs = new InputEntry[count];
        _setter = setter;
    }

    public Span<InputNumeric1> Values => _values.AsSpan();
    public Span<int> IntValues => _values.Reinterpret<int>().AsSpan();
    public Span<float> FloatValues => _values.Reinterpret<float>().AsSpan();

    public override bool Draw()
    {
        if (_count != _inputs.Length) Throwers.InvalidOperation(nameof(_count));

        ImGui.PushID(_id);

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
        Add(new InputEntry(StringArena.AllocateString(name), style, true, speed, min, max, fmt));

    public InputGroup WithIntInput(string name, InputStyle style, float speed, int min, int max) =>
        Add(new InputEntry(StringArena.AllocateString(name), style, false, speed, min, max));

    private InputGroup Add(InputEntry entry)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(_count, _inputs.Length);
        _inputs[_count++] = entry;
        return this;
    }


    private readonly struct InputEntry(
        NativeString label,
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
        private readonly bool _isFloat = isFloat;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Draw(InputNumeric1* value)
        {
            AppDraw.TextFrameAligned(_label);
            return _isFloat ? DrawFloat(value) : DrawInt(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DrawFloat(InputNumeric1* value)
        {
            var id = StringPacker.PackUtf8((byte)'#', (byte)'#', (byte)'i');
            return _style switch
            {
                InputStyle.Input => InputNumeric1.DrawFloatInput((byte*)&id, value, _format),
                InputStyle.Slider => InputNumeric1.DrawFloatSlider((byte*)&id, value, _format, _min, _max),
                InputStyle.Drag => InputNumeric1.DrawFloatDrag((byte*)&id, value, _format, _speed, _min, _max),
                _ => false
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DrawInt(InputNumeric1* value)
        {
            var id = StringPacker.PackUtf8((byte)'#', (byte)'#', (byte)'i');
            return _style switch
            {
                InputStyle.Input => InputNumeric1.DrawIntInput((byte*)&id, value),
                InputStyle.Slider => InputNumeric1.DrawIntSlider((byte*)&id, value, (int)_min, (int)_max),
                InputStyle.Drag => InputNumeric1.DrawIntDrag((byte*)&id, value, _speed, (int)_min, (int)_max),
                _ => false
            };
        }
    }
}*/