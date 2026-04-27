using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Theme;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;

internal abstract class UiField
{
    protected const int LabelAllocCapacity = 40;
    private static int _currentId = 1;

    private readonly byte[] _strId;

    public readonly int DrawId;
    public readonly string Label;

    public float Width;
    public FieldWidgetKind Widget { get; private set; }
    public FieldTrigger Trigger = FieldTrigger.OnChange;
    public FieldLayout Layout = FieldLayout.Top;

    protected UiField(string label, FieldWidgetKind widget)
    {
        Label = label;
        Widget = widget;
        DrawId = _currentId++;

        unsafe
        {
            var buffer = stackalloc byte[32];
            var written = new UnsafeSpanWriter(buffer, 32).Append("##ui").Append(DrawId).End();
            _strId = written.AsSpan().ToArray();
        }
    }

    public abstract ref byte GetRawValue();
    public abstract bool Draw();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe NativeView<byte> ApplyLabelLayout(byte* ptr)
    {
        var sw = new UnsafeSpanWriter(ptr, LabelAllocCapacity);

        switch (Layout)
        {
            case FieldLayout.Top:
                sw.Append(Label);
                AppDraw.Text(sw.End());
                ImGui.Separator();
                ImGui.PushItemWidth(GuiTheme.FormItemWidth);
                break;
            case FieldLayout.Inline:
                sw.Append(Label);
                ImGui.PushItemWidth(GuiTheme.FormItemInlineWidth);
                break;
        }

        return sw.Append(_strId).End();
    }


    protected bool ShouldTrigger()
    {
        return Trigger switch
        {
            FieldTrigger.OnChange => true,
            FieldTrigger.AfterChange => ImGui.IsItemDeactivatedAfterEdit(),
            FieldTrigger.AfterChangeDeactive => ImGui.IsItemDeactivatedAfterEdit() && !ImGui.IsItemActive(),
            _ => false
        };
    }
}