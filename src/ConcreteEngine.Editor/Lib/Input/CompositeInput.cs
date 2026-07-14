using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Lib.Inspection;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;
/*
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
            var format = it.Format;
            var label = TextBuffers.GetWriter().Write(it.Name);

            ImGui.PushID(i);
            changed |= it.Drawer.DrawFloat(1, label, valuePtr, (byte*)&format, it.Speed, it.Min, it.Max);
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


    private sealed class ComponentEntry(
        string name,
        InputStyle style,
        float speed,
        float min,
        float max,
        string format)
    {
        public readonly InputDrawer Drawer = InputDrawer.Get(style);

        public readonly byte[] Name = name.ToUtf8();
        public float Speed = speed, Min = min, Max = max;
        public String8Utf8 Format = format;
    }
}*/