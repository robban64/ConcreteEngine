using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Core.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;

internal sealed unsafe class FloatInputGroup : InputField
{
    public Float4 Value;
    private readonly Func<Float4> _getter;
    private readonly Action<Float4> _setter;

    private int _components;
    private readonly ComponentEntry[] _fields = new ComponentEntry[4];

    public FloatInputGroup(string label, Func<Float4> getter, Action<Float4> setter) : base(label, InputKind.Float)
    {
        _getter = getter;
        _setter = setter;
    }

    public bool Draw()
    {
        var value = Value = _getter();
        var valuePtr = (float*)&value;

        var changed = false;

        ImGui.PushID(DrawId);
        var len = int.Clamp(_components, 0, _fields.Length);
        for (var i = 0; i < len; ++i, ++valuePtr)
        {
            var it = _fields[i];
            var label = ScratchBuffer.Write(it.Name);

            ImGui.PushID(i);
            changed |= it.Style switch
            {
                InputStyle.Input => Float1.DrawInput(label, valuePtr, (byte*)&it.Format),
                InputStyle.Slider => Float1.DrawSlider(label, valuePtr, (byte*)&it.Format, it.Min, it.Max),
                InputStyle.Drag => Float1.DrawDrag(label, valuePtr, (byte*)&it.Format, it.Speed, it.Min, it.Max),
                _ => false
            };
            ImGui.PopID();
        }
        ImGui.PopID();

        if (changed && ShouldTrigger())
        {
            _setter(Value = value);
            return true;
        }

        return false;
    }

    public void AddInput(string name, InputStyle style, float speed, float min, float max, string format = "%.2f")
    {
        Add(new ComponentEntry(name, style, speed, min, max, format));
    }

    private FloatInputGroup Add(ComponentEntry entry)
    {
        ArgumentNullException.ThrowIfNull(_fields);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(_components, 4);

        _fields[_components] = entry;
        _components++;
        return this;
    }


    private struct ComponentEntry(
        string name,
        InputStyle style,
        float speed,
        float min,
        float max,
        string format)
    {
        public InputStyle Style = style;
        public float Speed = speed, Min = min, Max = max;
        public readonly byte[] Name = name.ToUtf8();
        public String8Utf8 Format = format;
    }
}