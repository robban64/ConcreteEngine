using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Inputs;

internal sealed unsafe class ColorInput : InputField
{
    public bool HasAlpha;

    public Color4 Value;

    private readonly Action<Color4> _setter;

    public ColorInput(string label, Action<Color4> setter, bool hasAlpha = true)
        : base(label, InputKind.Color)
    {
        _setter = setter;
        HasAlpha = hasAlpha;
    }

    public override bool Draw()
    {
        var strId = StringId;
        var value = Value;
        var changed = HasAlpha
            ? ImGui.ColorEdit4(strId._value, &value.R)
            : ImGui.ColorEdit3(strId._value, &value.R);

        if (changed && ShouldTrigger())
        {
            _setter(Value = value);
            return true;
        }

        return false;
    }
}