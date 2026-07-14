using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;

public enum InputTrigger : byte
{
    OnChange,
    AfterChange,
    AfterChangeDeactive
}

internal abstract class InputField
{
    private static int _idCounter;

    private readonly byte[] _label;

    public readonly int DrawId;
    public readonly InputKind Kind;
    public InputTrigger Trigger = InputTrigger.OnChange;
    public LabelPlacement LabelPlacement = LabelPlacement.Top;

    protected InputField(string label, InputKind kind)
    {
        DrawId = ++_idCounter;
        Kind = kind;
        _label = label.ToUtf8();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected NativeView<byte> ApplyLabelLayout(NativeSpanWriter sw)
    {
        if (LabelPlacement is LabelPlacement.Top)
        {
            AppDraw.Text(sw.Write(_label));
            ImGui.Separator();
            ImGui.SetNextItemWidth(GuiTheme.FormItemWidth);
        }
        else if (LabelPlacement is LabelPlacement.Inline)
        {
            sw.Append(_label);
            ImGui.SetNextItemWidth(GuiTheme.FormItemInlineWidth);
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