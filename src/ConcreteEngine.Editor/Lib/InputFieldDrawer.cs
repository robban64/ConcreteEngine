using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

internal readonly ref struct FloatDrawArg(
    ref byte label,
    ref float value,
    String8Utf8 format,
    float speed,
    float min,
    float max)
{
    public readonly ref byte Label = ref label;
    public readonly ref float Value = ref value;
    public readonly String8Utf8 Format = format;
    public readonly float Speed = speed;
    public readonly float Min = min;
    public readonly float Max = max;

}

internal readonly ref struct IntDrawArg(ref byte label, ref int value, int min, int max, float speed)
{
    public readonly ref byte Label = ref label;
    public readonly ref int Value = ref value;
    public readonly int Min = min;
    public readonly int Max = max;
    public readonly float Speed = speed;
}

internal static class InputFieldDrawer
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawInputFloat<T>(in FloatDrawArg args) where T : unmanaged, IFloatValue
    {
        var format = args.Format;
        return T.Components switch
        {
            1 => ImGui.InputFloat(ref args.Label, ref args.Value, ref format.GetRef()),
            2 => ImGui.InputFloat2(ref args.Label, ref args.Value, ref format.GetRef()),
            3 => ImGui.InputFloat3(ref args.Label, ref args.Value, ref format.GetRef()),
            4 => ImGui.InputFloat4(ref args.Label, ref args.Value, ref format.GetRef()),
            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawSliderFloat<T>(in FloatDrawArg args) where T : unmanaged, IFloatValue
    {
        var format = args.Format;
        return T.Components switch
        {
            1 => ImGui.SliderFloat(ref args.Label, ref args.Value, args.Min, args.Max, ref format.GetRef()),
            2 => ImGui.SliderFloat2(ref args.Label, ref args.Value, args.Min, args.Max, ref format.GetRef()),
            3 => ImGui.SliderFloat3(ref args.Label, ref args.Value, args.Min, args.Max, ref format.GetRef()),
            4 => ImGui.SliderFloat4(ref args.Label, ref args.Value, args.Min, args.Max, ref format.GetRef()),
            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawDragFloat<T>(in FloatDrawArg args) where T : unmanaged, IFloatValue
    {
        var format = args.Format;
        return T.Components switch
        {
            1 => ImGui.DragFloat(ref args.Label, ref args.Value, args.Speed, args.Min, args.Max, ref format.GetRef()),
            2 => ImGui.DragFloat2(ref args.Label, ref args.Value, args.Speed, args.Min, args.Max, ref format.GetRef()),
            3 => ImGui.DragFloat3(ref args.Label, ref args.Value, args.Speed, args.Min, args.Max, ref format.GetRef()),
            4 => ImGui.DragFloat4(ref args.Label, ref args.Value, args.Speed, args.Min, args.Max, ref format.GetRef()),
            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawInputInt<T>(in IntDrawArg ctx) where T : unmanaged, IIntValue =>
        T.Components switch
        {
            1 => ImGui.InputInt(ref ctx.Label, ref ctx.Value),
            2 => ImGui.InputInt2(ref ctx.Label, ref ctx.Value),
            3 => ImGui.InputInt3(ref ctx.Label, ref ctx.Value),
            4 => ImGui.InputInt4(ref ctx.Label, ref ctx.Value),
            _ => false
        };

    public static bool DrawSliderInt<T>(in IntDrawArg ctx) where T : unmanaged, IIntValue =>
        T.Components switch
        {
            1 => ImGui.SliderInt(ref ctx.Label, ref ctx.Value, ctx.Min, ctx.Max),
            2 => ImGui.SliderInt2(ref ctx.Label, ref ctx.Value, ctx.Min, ctx.Max),
            3 => ImGui.SliderInt3(ref ctx.Label, ref ctx.Value, ctx.Min, ctx.Max),
            4 => ImGui.SliderInt4(ref ctx.Label, ref ctx.Value, ctx.Min, ctx.Max),
            _ => false
        };

    public static bool DrawDragInt<T>(in IntDrawArg ctx) where T : unmanaged, IIntValue =>
        T.Components switch
        {
            1 => ImGui.DragInt(ref ctx.Label, ref ctx.Value, ctx.Speed, ctx.Min, ctx.Max),
            2 => ImGui.DragInt2(ref ctx.Label, ref ctx.Value, ctx.Speed, ctx.Min, ctx.Max),
            3 => ImGui.DragInt3(ref ctx.Label, ref ctx.Value, ctx.Speed, ctx.Min, ctx.Max),
            4 => ImGui.DragInt4(ref ctx.Label, ref ctx.Value, ctx.Speed, ctx.Min, ctx.Max),
            _ => false
        };
}