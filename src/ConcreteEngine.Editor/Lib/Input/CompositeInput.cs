using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Lib.Inspection;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;
/*
internal sealed unsafe class CompositeInput(string label) : InputField(label, InputKind.Float)
{
    public Float4 Value;
    private int _components;
    private readonly ComponentEntry[] _fields = new ComponentEntry[4];

    public bool Draw()
    {
        var value = Value;
        var valuePtr = (float*)&value;

        var changed = false;
        
        ImGui.PushID(DrawId);
        var len = int.Min(_components, _fields.Length);
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

        changed &= ShouldTrigger();
        if (changed) Value = value;
        return changed;
    }

    public void AddInput(string name, InputStyle style, float speed, float min, float max, string format = "%.2f")
    {
        Add(new ComponentEntry(name, style, speed, min, max, format));
    }

    private void Add(ComponentEntry entry)
    {
        ArgumentNullException.ThrowIfNull(_fields);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(_components, 4);

        _fields[_components] = entry;
        _components++;
    }


    private sealed class ComponentEntry(
        string name,
        InputStyle style,
        float speed,
        float min,
        float max,
        string format)
    {
        public readonly InputDrawer Drawer = InputDrawer.Bind(style);

        public readonly byte[] Name = name.ToUtf8();
        public float Speed = speed, Min = min, Max = max;
        
        public String8Utf8 Format = format;
    }
}
*/