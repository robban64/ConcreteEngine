using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

internal static class InputFieldDrawer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawInputFloat(int c, ref byte label, ref float value, ref byte format, float speed, float min,
        float max)
    {
        return c switch
        {
            1 => ImGui.InputFloat(ref label, ref value, ref format),
            2 => ImGui.InputFloat2(ref label, ref value, ref format),
            3 => ImGui.InputFloat3(ref label, ref value, ref format),
            4 => ImGui.InputFloat4(ref label, ref value, ref format),
            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawSliderFloat(int c, ref byte label, ref float value, ref byte format, float speed,
        float min, float max)
    {
        return c switch
        {
            1 => ImGui.SliderFloat(ref label, ref value, min, max, ref format),
            2 => ImGui.SliderFloat2(ref label, ref value, min, max, ref format),
            3 => ImGui.SliderFloat3(ref label, ref value, min, max, ref format),
            4 => ImGui.SliderFloat4(ref label, ref value, min, max, ref format),
            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawDragFloat(int c, ref byte label, ref float value, ref byte format, float speed,
        float min, float max)
    {
        return c switch
        {
            1 => ImGui.DragFloat(ref label, ref value, speed, min, max, ref format),
            2 => ImGui.DragFloat2(ref label, ref value, speed, min, max, ref format),
            3 => ImGui.DragFloat3(ref label, ref value, speed, min, max, ref format),
            4 => ImGui.DragFloat4(ref label, ref value, speed, min, max, ref format),
            _ => false
        };
    }

    public static bool DrawInputInt(int c, ref byte label, ref int value, float speed, int min, int max) => c switch
    {
        1 => ImGui.InputInt(ref label, ref value),
        2 => ImGui.InputInt2(ref label, ref value),
        3 => ImGui.InputInt3(ref label, ref value),
        4 => ImGui.InputInt4(ref label, ref value),
        _ => false
    };

    public static bool DrawSliderInt(int c, ref byte label, ref int value, float speed, int min, int max) => c switch
    {
        1 => ImGui.SliderInt(ref label, ref value, min, max),
        2 => ImGui.SliderInt2(ref label, ref value, min, max),
        3 => ImGui.SliderInt3(ref label, ref value, min, max),
        4 => ImGui.SliderInt4(ref label, ref value, min, max),
        _ => false
    };

    public static bool DrawDragInt(int c, ref byte label, ref int value, float speed, int min, int max) => c switch
    {
        1 => ImGui.DragInt(ref label, ref value, speed, min, max),
        2 => ImGui.DragInt2(ref label, ref value, speed, min, max),
        3 => ImGui.DragInt3(ref label, ref value, speed, min, max),
        4 => ImGui.DragInt4(ref label, ref value, speed, min, max),
        _ => false
    };
}