using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Lib.Inspection;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;


public enum InputTrigger : byte
{
    OnChange,
    AfterChange,
    AfterChangeDeactive
}



internal abstract class InputField
{
    private static int _currentId = 1;

    private readonly byte[] _label;

    public readonly int DrawId;
    public readonly InputKind Kind;
    public InputTrigger Trigger = InputTrigger.OnChange;
    public LabelPlacement LabelPlacement = LabelPlacement.Top;

    protected InputField(string label, InputKind kind)
    {
        DrawId = _currentId++;
        Kind = kind;
        _label = label.ToUtf8();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected NativeView<byte> ApplyLabelLayout(NativeSpanWriter sw)
    {
        if (LabelPlacement is LabelPlacement.Top or LabelPlacement.Inline)
            sw.Append(_label);

        if (LabelPlacement is LabelPlacement.Top)
        {
            AppDraw.Text(sw.End());
            ImGui.Separator();
        }

        return sw.AppendImGuiId(DrawId).End();
    }


    protected bool ShouldTrigger()
    {
        return Trigger switch
        {
            InputTrigger.OnChange => true,
            InputTrigger.AfterChange => ImGui.IsItemDeactivatedAfterEdit(),
            InputTrigger.AfterChangeDeactive => ImGui.IsItemDeactivatedAfterEdit() && !ImGui.IsItemActive(),
            _ => false
        };
    }
}