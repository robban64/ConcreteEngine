using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Lib.Field;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Inspection;

internal static unsafe class InputFieldDrawer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool InputFloat(byte* label, float* value, byte* fmt, float speed, float min, float max) =>
        ImGui.InputFloat(label, value, fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool InputFloat2(byte* label, float* value, byte* fmt, float speed, float min, float max) =>
        ImGui.InputFloat2(label, value, fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool InputFloat3(byte* label, float* value, byte* fmt, float speed, float min, float max) =>
        ImGui.InputFloat3(label, value, fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool InputFloat4(byte* label, float* value, byte* fmt, float speed, float min, float max) =>
        ImGui.InputFloat4(label, value, fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool SliderFloat(byte* label, float* value, byte* fmt, float speed, float min, float max) =>
        ImGui.SliderFloat(label, value, min, max, fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool SliderFloat2(byte* label, float* value, byte* fmt, float speed, float min, float max) =>
        ImGui.SliderFloat2(label, value, min, max, fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool SliderFloat3(byte* label, float* value, byte* fmt, float speed, float min, float max) =>
        ImGui.SliderFloat3(label, value, min, max, fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool SliderFloat4(byte* label, float* value, byte* fmt, float speed, float min, float max) =>
        ImGui.SliderFloat4(label, value, min, max, fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool DragFloat(byte* label, float* value, byte* fmt, float speed, float min, float max) =>
        ImGui.DragFloat(label, value, speed, min, max, fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool DragFloat2(byte* label, float* value, byte* fmt, float speed, float min, float max) =>
        ImGui.DragFloat2(label, value, speed, min, max, fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool DragFloat3(byte* label, float* value, byte* fmt, float speed, float min, float max) =>
        ImGui.DragFloat3(label, value, speed, min, max, fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool DragFloat4(byte* label, float* value, byte* fmt, float speed, float min, float max) =>
        ImGui.DragFloat4(label, value, speed, min, max, fmt);


    public static delegate*<byte*, float*, byte*, float, float, float, bool> BindFloat2(InputStyle kind,
        int component)
    {
        switch (kind)
        {
            case InputStyle.Input:
                return component switch
                {
                    1 => &InputFloat,
                    2 => &InputFloat2,
                    3 => &InputFloat3,
                    4 => &InputFloat4,
                    _ => throw new ArgumentOutOfRangeException(nameof(component))
                };
            case InputStyle.Slider:
                return component switch
                {
                    1 => &SliderFloat,
                    2 => &SliderFloat2,
                    3 => &SliderFloat3,
                    4 => &SliderFloat4,
                    _ => throw new ArgumentOutOfRangeException(nameof(component))
                };
            case InputStyle.Drag:
                return component switch
                {
                    1 => &DragFloat,
                    2 => &DragFloat2,
                    3 => &DragFloat3,
                    4 => &DragFloat4,
                    _ => throw new ArgumentOutOfRangeException(nameof(component))
                };
            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    private static bool InputInt(byte* label, int* value, float speed, int min, int max) =>
        ImGui.InputInt(label, value);

    private static bool InputInt2(byte* label, int* value, float speed, int min, int max) =>
        ImGui.InputInt2(label, value);

    private static bool InputInt3(byte* label, int* value, float speed, int min, int max) =>
        ImGui.InputInt3(label, value);

    private static bool InputInt4(byte* label, int* value, float speed, int min, int max) =>
        ImGui.InputInt4(label, value);

    private static bool SliderInt(byte* label, int* value, float speed, int min, int max) =>
        ImGui.SliderInt(label, value, min, max);

    private static bool SliderInt2(byte* label, int* value, float speed, int min, int max) =>
        ImGui.SliderInt2(label, value, min, max);

    private static bool SliderInt3(byte* label, int* value, float speed, int min, int max) =>
        ImGui.SliderInt3(label, value, min, max);

    private static bool SliderInt4(byte* label, int* value, float speed, int min, int max) =>
        ImGui.SliderInt4(label, value, min, max);


    private static bool DragInt(byte* label, int* value, float speed, int min, int max) =>
        ImGui.DragInt(label, value, speed, min, max);

    private static bool DragInt2(byte* label, int* value, float speed, int min, int max) =>
        ImGui.DragInt2(label, value, speed, min, max);

    private static bool DragInt3(byte* label, int* value, float speed, int min, int max) =>
        ImGui.DragInt3(label, value, speed, min, max);

    private static bool DragInt4(byte* label, int* value, float speed, int min, int max) =>
        ImGui.DragInt4(label, value, speed, min, max);

    public static delegate*<byte*, int*, float, int, int, bool> BindInt2(InputStyle kind, int component)
    {
        switch (kind)
        {
            case InputStyle.Input:
                return component switch
                {
                    1 => &InputInt,
                    2 => &InputInt2,
                    3 => &InputInt3,
                    4 => &InputInt4,
                    _ => throw new ArgumentOutOfRangeException(nameof(component))
                };
            case InputStyle.Slider:
                return component switch
                {
                    1 => &SliderInt,
                    2 => &SliderInt2,
                    3 => &SliderInt3,
                    4 => &SliderInt4,
                    _ => throw new ArgumentOutOfRangeException(nameof(component))
                };
            case InputStyle.Drag:
                return component switch
                {
                    1 => &DragInt,
                    2 => &DragInt2,
                    3 => &DragInt3,
                    4 => &DragInt4,
                    _ => throw new ArgumentOutOfRangeException(nameof(component))
                };
            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static delegate*<int, byte*, float*, byte*, float, float, float, bool> BindFloat(InputFieldKind kind)
    {
        return kind switch
        {
            InputFieldKind.Input => &DrawInputFloat,
            InputFieldKind.Slider => &DrawSliderFloat,
            InputFieldKind.Drag => &DrawDragFloat,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static delegate*<int, byte*, int*, float, int, int, bool> BindInt(InputFieldKind kind)
    {
        return kind switch
        {
            InputFieldKind.Input => &DrawInputInt,
            InputFieldKind.Slider => &DrawSliderInt,
            InputFieldKind.Drag => &DrawDragInt,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
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
    public static bool DrawSliderFloat(int c, byte* label, float* value, byte* format, float speed, float min,
        float max)
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