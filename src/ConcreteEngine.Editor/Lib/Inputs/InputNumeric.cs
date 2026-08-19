using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Inputs;

internal interface IInputNumeric<T> where T : unmanaged, IInputNumeric<T>
{
    static abstract byte Components { get; }

    static abstract unsafe bool DrawFloatInput(byte* str, T* v, String8Utf8 fmt);
    static abstract unsafe bool DrawFloatSlider(byte* str, T* v, String8Utf8 fmt, float min, float max);
    static abstract unsafe bool DrawFloatDrag(byte* str, T* v, String8Utf8 fmt, float speed, float min, float max);

    static abstract unsafe bool DrawIntInput(byte* str, T* v);
    static abstract unsafe bool DrawIntSlider(byte* str, T* v, int min, int max);
    static abstract unsafe bool DrawIntDrag(byte* str, T* v, float speed, int min, int max);
}

[StructLayout(LayoutKind.Explicit)]
internal struct InputNumeric1 : IInputNumeric<InputNumeric1>
{
    [FieldOffset(0)] public int I1;
    [FieldOffset(0)] public float F1;

    public InputNumeric1(int v) => I1 = v;
    public InputNumeric1(float v) => F1 = v;

    public static implicit operator InputNumeric1(int v) => new(v);
    public static implicit operator InputNumeric1(float v) => new(v);
    public static explicit operator int(InputNumeric1 v) => v.I1;
    public static explicit operator float(InputNumeric1 v) => v.F1;

    public static byte Components => 1;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawFloatInput(byte* str, InputNumeric1* v, String8Utf8 fmt) =>
        ImGui.InputFloat(str, &v->F1, (byte*)&fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawFloatSlider(byte* str, InputNumeric1* v, String8Utf8 fmt, float min, float max) =>
        ImGui.SliderFloat(str, &v->F1, min, max, (byte*)&fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawFloatDrag(byte* str, InputNumeric1* v, String8Utf8 fmt, float speed, float min,
        float max) =>
        ImGui.DragFloat(str, &v->F1, speed, min, max, (byte*)&fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawIntInput(byte* str, InputNumeric1* v) => ImGui.InputInt(str, &v->I1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawIntSlider(byte* str, InputNumeric1* v, int min, int max) =>
        ImGui.SliderInt(str, &v->I1, min, max);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawIntDrag(byte* str, InputNumeric1* v, float speed, int min, int max) =>
        ImGui.DragInt(str, &v->I1, speed, min, max);
}

[StructLayout(LayoutKind.Explicit)]
internal struct InputNumeric2 : IInputNumeric<InputNumeric2>
{
    [FieldOffset(00)] public int I1;
    [FieldOffset(04)] public int I2;

    [FieldOffset(00)] public float F1;
    [FieldOffset(04)] public float F2;

    public static byte Components => 2;

    public static implicit operator InputNumeric2(Int2 v) => Unsafe.BitCast<Int2, InputNumeric2>(v);
    public static implicit operator InputNumeric2(Vector2 v) => Unsafe.BitCast<Vector2, InputNumeric2>(v);
    public static explicit operator Int2(InputNumeric2 v) => Unsafe.BitCast<InputNumeric2, Int2>(v);
    public static explicit operator Vector2(InputNumeric2 v) => Unsafe.BitCast<InputNumeric2, Vector2>(v);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawFloatInput(byte* str, InputNumeric2* v, String8Utf8 fmt) =>
        ImGui.InputFloat2(str, &v->F1, (byte*)&fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawFloatSlider(byte* str, InputNumeric2* v, String8Utf8 fmt, float min, float max) =>
        ImGui.SliderFloat2(str, &v->F1, min, max, (byte*)&fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawFloatDrag(byte* str, InputNumeric2* v, String8Utf8 fmt, float speed, float min,
        float max) =>
        ImGui.DragFloat2(str, &v->F1, speed, min, max, (byte*)&fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawIntInput(byte* str, InputNumeric2* v) => ImGui.InputInt2(str, &v->I1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawIntSlider(byte* str, InputNumeric2* v, int min, int max) =>
        ImGui.SliderInt2(str, &v->I1, min, max);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawIntDrag(byte* str, InputNumeric2* v, float speed, int min, int max) =>
        ImGui.DragInt2(str, &v->I1, speed, min, max);
}

[StructLayout(LayoutKind.Explicit)]
internal struct InputNumeric3 : IInputNumeric<InputNumeric3>
{
    [FieldOffset(00)] public int I1;
    [FieldOffset(04)] public int I2;
    [FieldOffset(08)] public int I3;

    [FieldOffset(00)] public float F1;
    [FieldOffset(04)] public float F2;
    [FieldOffset(08)] public float F3;

    public static byte Components => 3;

    public static implicit operator InputNumeric3(Int3 v) => Unsafe.BitCast<Int3, InputNumeric3>(v);
    public static implicit operator InputNumeric3(Vector3 v) => Unsafe.BitCast<Vector3, InputNumeric3>(v);
    public static explicit operator Int3(InputNumeric3 v) => Unsafe.BitCast<InputNumeric3, Int3>(v);
    public static explicit operator Vector3(InputNumeric3 v) => Unsafe.BitCast<InputNumeric3, Vector3>(v);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawFloatInput(byte* str, InputNumeric3* v, String8Utf8 fmt) =>
        ImGui.InputFloat3(str, &v->F1, (byte*)&fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawFloatSlider(byte* str, InputNumeric3* v, String8Utf8 fmt, float min, float max) =>
        ImGui.SliderFloat3(str, &v->F1, min, max, (byte*)&fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawFloatDrag(byte* str, InputNumeric3* v, String8Utf8 fmt, float speed, float min,
        float max) =>
        ImGui.DragFloat3(str, &v->F1, speed, min, max, (byte*)&fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawIntInput(byte* str, InputNumeric3* v) => ImGui.InputInt3(str, &v->I1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawIntSlider(byte* str, InputNumeric3* v, int min, int max) =>
        ImGui.SliderInt3(str, &v->I1, min, max);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawIntDrag(byte* str, InputNumeric3* v, float speed, int min, int max) =>
        ImGui.DragInt3(str, &v->I1, speed, min, max);
}

[StructLayout(LayoutKind.Explicit)]
internal struct InputNumeric4 : IInputNumeric<InputNumeric4>
{
    [FieldOffset(00)] public int I1;
    [FieldOffset(04)] public int I2;
    [FieldOffset(08)] public int I3;
    [FieldOffset(12)] public int I4;

    [FieldOffset(00)] public float F1;
    [FieldOffset(04)] public float F2;
    [FieldOffset(08)] public float F3;
    [FieldOffset(12)] public float F4;

    public static byte Components => 4;

    public static implicit operator InputNumeric4(Int4 v) => Unsafe.BitCast<Int4, InputNumeric4>(v);
    public static implicit operator InputNumeric4(Vector4 v) => Unsafe.BitCast<Vector4, InputNumeric4>(v);
    public static explicit operator Int4(InputNumeric4 v) => Unsafe.BitCast<InputNumeric4, Int4>(v);
    public static explicit operator Vector4(InputNumeric4 v) => Unsafe.BitCast<InputNumeric4, Vector4>(v);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawFloatInput(byte* str, InputNumeric4* v, String8Utf8 fmt) =>
        ImGui.InputFloat4(str, &v->F1, (byte*)&fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawFloatSlider(byte* str, InputNumeric4* v, String8Utf8 fmt, float min, float max) =>
        ImGui.SliderFloat4(str, &v->F1, min, max, (byte*)&fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawFloatDrag(byte* str, InputNumeric4* v, String8Utf8 fmt, float speed, float min,
        float max) =>
        ImGui.DragFloat4(str, &v->F1, speed, min, max, (byte*)&fmt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawIntInput(byte* str, InputNumeric4* v) => ImGui.InputInt4(str, &v->I1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawIntSlider(byte* str, InputNumeric4* v, int min, int max) =>
        ImGui.SliderInt4(str, &v->I1, min, max);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawIntDrag(byte* str, InputNumeric4* v, float speed, int min, int max) =>
        ImGui.DragInt4(str, &v->I1, speed, min, max);
}