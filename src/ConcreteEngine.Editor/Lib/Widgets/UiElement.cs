using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Theme;
using Hexa.NET.ImGui;
using Silk.NET.Input;

namespace ConcreteEngine.Editor.Lib.Widgets;

internal abstract class UiElement
{
    protected const int LabelAllocCapacity = 40;
    private static int _currentId = 1;

    private readonly byte[] _strId;

    public readonly int DrawId;
    public readonly string Label;

    public float Width;
    public FieldWidgetKind Widget { get; private set; }

    public FieldTrigger Trigger;
    public FieldLayout Layout = FieldLayout.Top;

    protected UiElement(string label, FieldWidgetKind widget)
    {
        Label = label;
        Widget = widget;
        DrawId = _currentId++;

        unsafe
        {
            var buffer = stackalloc byte[32];
            var written = new UnsafeSpanWriter(buffer,32).Append("##ui").Append(DrawId).End();
            _strId = written.AsSpan().ToArray();
        }
    }

    public abstract bool Draw();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe NativeView<byte> DrawWriteLabel(byte* ptr)
    {
        var sw = new UnsafeSpanWriter(ptr, LabelAllocCapacity);
        if (Label.Length > 0)
        {
            if (Layout != FieldLayout.None)
                sw.Append(Label);

            if (Layout == FieldLayout.Top)
                AppDraw.Text(sw.End());
        }

        return sw.Append(_strId).End();
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
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