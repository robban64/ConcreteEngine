using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;


internal sealed unsafe class ColorInput : InputField
{
    public bool HasAlpha;

    private readonly Color4* _value;

    private readonly Action<Color4> _setter;
    
    public ColorInput(string label, Action<Color4> setter, bool hasAlpha = true)
        : base(label, InputKind.Color)
    {
        _setter = setter;
        HasAlpha = hasAlpha;
        LabelPlacement = LabelPlacement.Top;
        _value = (Color4*)StringArena.Instance.AllocBytes(Unsafe.SizeOf<Color4>()).Ptr;
    }
    
    public ref Color4 Value => ref *_value;

    public bool Draw()
    {
        DrawLabel();
        var changed = HasAlpha
            ? ImGui.ColorEdit4(StringId, &_value->R)
            : ImGui.ColorEdit3(StringId, &_value->R);

        if (changed && ShouldTrigger())
        {
            _setter(*_value);
            return true;
        }

        return false;
    }
}