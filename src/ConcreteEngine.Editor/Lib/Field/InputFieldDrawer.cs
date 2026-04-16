using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;

internal static unsafe class InputFieldDrawer
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static delegate*<int, byte*, float*, byte*, float, float, float, bool> BindFloat(FieldWidgetKind widgetKind)
    {
        return widgetKind switch
        {
            FieldWidgetKind.Input => &DrawInputFloat,
            FieldWidgetKind.Slider => &DrawSliderFloat,
            FieldWidgetKind.Drag => &DrawDragFloat,
            _ => throw new ArgumentOutOfRangeException(nameof(widgetKind), widgetKind, null)
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static delegate*<int, byte*, int*, float, int, int, bool> BindInt(FieldWidgetKind widgetKind)
    {
        return widgetKind switch
        {
            FieldWidgetKind.Input => &DrawInputInt,
            FieldWidgetKind.Slider => &DrawSliderInt,
            FieldWidgetKind.Drag => &DrawDragInt,
            _ => throw new ArgumentOutOfRangeException(nameof(widgetKind), widgetKind, null)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawInputFloat(int c, byte* label, float* value, byte* format, float speed, float min, float max)
    {
        return c switch
        {
            1 => ImGui.InputFloat(label, value, format),
            2 => ImGui.InputFloat2(label, value, format),
            3 => ImGui.InputFloat3(label, value, format),
            4 => ImGui.InputFloat4(label, value, format),
            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawSliderFloat(int c, byte* label, float* value, byte* format, float speed, float min, float max)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawDragFloat(int c, byte* label, float* value, byte* format, float speed,
        float min, float max)
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

    public static bool DrawInputInt(int c, byte* label, int* value, float speed, int min, int max) =>
        c switch
        {
            1 => ImGui.InputInt(label, value),
            2 => ImGui.InputInt2(label, value),
            3 => ImGui.InputInt3(label, value),
            4 => ImGui.InputInt4(label, value),
            _ => false
        };

    public static bool DrawSliderInt(int c, byte* label, int* value, float speed, int min, int max) =>
        c switch
        {
            1 => ImGui.SliderInt(label, value, min, max),
            2 => ImGui.SliderInt2(label, value, min, max),
            3 => ImGui.SliderInt3(label, value, min, max),
            4 => ImGui.SliderInt4(label, value, min, max),
            _ => false
        };

    public static bool DrawDragInt(int c, byte* label, int* value, float speed, int min, int max) =>
        c switch
        {
            1 => ImGui.DragInt(label, value, speed, min, max),
            2 => ImGui.DragInt2(label, value, speed, min, max),
            3 => ImGui.DragInt3(label, value, speed, min, max),
            4 => ImGui.DragInt4(label, value, speed, min, max),
            _ => false
        };
}