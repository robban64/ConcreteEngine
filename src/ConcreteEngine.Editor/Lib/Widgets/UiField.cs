using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Lib.Inspection;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;

internal abstract class UiField
{
    protected const int LabelAllocCapacity = 40;
    private static int _currentId = 1;

    public readonly int DrawId;
    public readonly string Label;

    public float Width;
    public FieldKind Widget { get; private set; }
    public InputTrigger Trigger = InputTrigger.OnChange;
    public FieldLabelPlacement LabelPlacement = FieldLabelPlacement.Top;

    protected UiField(string label, FieldKind widget)
    {
        Label = label;
        Widget = widget;
        DrawId = _currentId++;
    }

    public abstract ref byte GetRawValue();
    public abstract bool Draw();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected  NativeView<byte> ApplyLabelLayout(NativeSpanWriter sw)
    {
        switch (LabelPlacement)
        {
            case FieldLabelPlacement.Top:
                sw.Append(Label);
                AppDraw.Text(sw.End());
                ImGui.Separator();
                ImGui.PushItemWidth(GuiTheme.FormItemWidth);
                break;
            case FieldLabelPlacement.Inline:
                sw.Append(Label);
                ImGui.PushItemWidth(GuiTheme.FormItemInlineWidth);
                break;
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