using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Inputs;

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
        _value = (Color4*)StringArena.Instance.AllocBytes(Unsafe.SizeOf<Color4>()).Ptr;
    }

    public ref Color4 Value => ref *_value;

    public override bool Draw()
    {
        var strId = _stringId;
        var strIdPtr = strId._value;
        var changed = HasAlpha
            ? ImGui.ColorEdit4(strIdPtr, &_value->R)
            : ImGui.ColorEdit3(strIdPtr, &_value->R);

        if (changed && ShouldTrigger())
        {
            _setter(*_value);
            return true;
        }

        return false;
    }
}