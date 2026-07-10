using ConcreteEngine.Core.Engine.Editor;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;

internal abstract class InputDrawer
{
    public abstract unsafe bool DrawFloat(int c, byte* label, float* value, byte* format, float speed, float min, float max);
    public abstract unsafe bool DrawInt(int c, byte* label, int* value, float speed, int min, int max);
    
    public static InputDrawer Bind(InputStyle style) => style switch
    {
        InputStyle.Input  => DefaultInputDrawer.Instance,
        InputStyle.Slider => SliderDrawer.Instance,
        InputStyle.Drag   => DragDrawer.Instance,
        _ => throw new ArgumentOutOfRangeException(nameof(style), style, null)
    };
}

internal sealed unsafe class DefaultInputDrawer : InputDrawer
{
    public static readonly DefaultInputDrawer Instance = new();
    private DefaultInputDrawer() { }
    public override bool DrawFloat(int c, byte* label, float* value, byte* format, float speed, float min, float max)
    {
        return c switch
        {
            1 => ImGui.InputFloat(label, value,  format),
            2 => ImGui.InputFloat2(label, value, format),
            3 => ImGui.InputFloat3(label, value, format),
            4 => ImGui.InputFloat4(label, value, format),
            _ => false
        };
    }
    public override bool DrawInt(int c, byte* label, int* value, float speed, int min, int max)
    {
        return c switch
        {
            1 => ImGui.InputInt(label, value),
            2 => ImGui.InputInt2(label, value),
            3 => ImGui.InputInt3(label, value),
            4 => ImGui.InputInt4(label, value),
            _ => false
        };
    }

}

internal sealed unsafe  class SliderDrawer : InputDrawer
{
    public static readonly SliderDrawer Instance = new();
    private SliderDrawer() { }
    public override bool DrawFloat(int c, byte* label, float* value, byte* format, float speed, float min, float max)
    {
        return c switch
        {
            1 => ImGui.SliderFloat(label, value, min, max, format),
            2 => ImGui.SliderFloat2(label, value, min, max, format),
            3 => ImGui.SliderFloat3(label, value, min, max, format),
            4 => ImGui.SliderFloat4(label, value, min, max, format),
            _ => false
        };
    }
    public override bool DrawInt(int c, byte* label, int* value, float speed, int min, int max)
    {
        return c switch
        {
            1 => ImGui.SliderInt(label, value, min, max),
            2 => ImGui.SliderInt2(label, value, min, max),
            3 => ImGui.SliderInt3(label, value, min, max),
            4 => ImGui.SliderInt4(label, value, min, max),
            _ => false
        };
    }

}

internal sealed unsafe class DragDrawer : InputDrawer
{
    public static readonly DragDrawer Instance = new();
    private DragDrawer() { }
    public override bool DrawFloat(int c, byte* label, float* value, byte* format, float speed, float min, float max)
    {
        return c switch
        {
            1 => ImGui.DragFloat(label, value, speed, min, max, format),
            2 => ImGui.DragFloat2(label, value, speed, min, max, format),
            3 => ImGui.DragFloat3(label, value, speed, min, max, format),
            4 => ImGui.DragFloat4(label, value, speed, min, max, format),
            _ => false
        };
    }
    public override bool DrawInt(int c, byte* label, int* value, float speed, int min, int max)
    {
        return c switch
        {
            1 => ImGui.DragInt(label, value, speed, min, max),
            2 => ImGui.DragInt2(label, value, speed, min, max),
            3 => ImGui.DragInt3(label, value, speed, min, max),
            4 => ImGui.DragInt4(label, value, speed, min, max),
            _ => false
        };
    }
}
