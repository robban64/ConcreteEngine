using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;

public interface IFloatValue
{
    [UnscopedRef]
    ref float Ref();

    static abstract int Components { get; }
    static abstract unsafe bool DrawInput(byte* label, float* value, byte* format);
    static abstract unsafe bool DrawSlider(byte* label, float* value, byte* format, float min, float max);
    static abstract unsafe bool DrawDrag(byte* label, float* value, byte* format, float speed, float min, float max);
}

[StructLayout(LayoutKind.Sequential)]
public struct Float1(float x) : IFloatValue
{
    public float X = x;

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref float Ref() => ref X;

    public static implicit operator Float1(float v) => new(v);
    public static explicit operator float(Float1 v) => v.X;

    public static int Components => 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawInput(byte* label, float* value, byte* format) =>
        ImGui.InputFloat(label, value, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawSlider(byte* label, float* value, byte* format, float min, float max) =>
        ImGui.SliderFloat(label, value, min, max, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawDrag(byte* label, float* value, byte* format, float speed, float min, float max) =>
        ImGui.DragFloat(label, value, speed, min, max, format);
}

[StructLayout(LayoutKind.Sequential)]
public struct Float2(float x, float y) : IFloatValue
{
    public float X = x, Y = y;

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref float Ref() => ref X;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Float2(Vector2 v) => new(v.X, v.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector2(Float2 v) => new(v.X, v.Y);

    public static int Components => 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawInput(byte* label, float* value, byte* format) =>
        ImGui.InputFloat2(label, value, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawSlider(byte* label, float* value, byte* format, float min, float max) =>
        ImGui.SliderFloat2(label, value, min, max, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawDrag(byte* label, float* value, byte* format, float speed, float min, float max) =>
        ImGui.DragFloat2(label, value, speed, min, max, format);
}

[StructLayout(LayoutKind.Sequential)]
public struct Float3(float x, float y, float z) : IFloatValue
{
    public float X = x, Y = y, Z = z;

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref float Ref() => ref X;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Float3(Vector3 v) => new(v.X, v.Y, v.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector3(Float3 v) => new(v.X, v.Y, v.Z);

    public static int Components => 3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawInput(byte* label, float* value, byte* format) =>
        ImGui.InputFloat3(label, value, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawSlider(byte* label, float* value, byte* format, float min, float max) =>
        ImGui.SliderFloat3(label, value, min, max, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawDrag(byte* label, float* value, byte* format, float speed, float min, float max) =>
        ImGui.DragFloat3(label, value, speed, min, max, format);
}

[StructLayout(LayoutKind.Sequential)]
public struct Float4(float x, float y, float z, float w = 0f) : IFloatValue
{
    public float X = x, Y = y, Z = z, W = w;

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref float Ref() => ref X;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Float4(in Vector4 v) => new(v.X, v.Y, v.Z, v.W);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector4(in Float4 v) => new(v.X, v.Y, v.Z, v.W);

    public static int Components => 4;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawInput(byte* label, float* value, byte* format) =>
        ImGui.InputFloat4(label, value, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawSlider(byte* label, float* value, byte* format, float min, float max) =>
        ImGui.SliderFloat4(label, value, min, max, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawDrag(byte* label, float* value, byte* format, float speed, float min, float max) =>
        ImGui.DragFloat4(label, value, speed, min, max, format);


/*
    [UnscopedRef,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnsafeFrom<T>(T value) where T : IFloatValue { Unsafe.As<Float4, T>(ref this) = value;}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Float4(in Quaternion v) => new(v.X, v.Y, v.Z, v.W);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Float4(in Color4 v) => new(v.R, v.G, v.B, v.A);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector3(in Float4 v) => new(v.X, v.Y, v.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Quaternion(in Float4 v) => new(v.X, v.Y, v.Z, v.W);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Color4(in Float4 v) => new(v.X, v.Y, v.Z, v.W);
*/
}