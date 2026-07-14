using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;


public interface IIntValue
{
    [UnscopedRef]
    ref int Ref();
    static abstract int Components { get; }
    static abstract unsafe bool DrawInput(byte* label, int* value);
    static abstract unsafe bool DrawSlider(byte* label, int* value, int min, int max);
    static abstract unsafe bool DrawDrag(byte* label, int* value, float speed, int min, int max);
}


[StructLayout(LayoutKind.Sequential)]
public struct Int1(int x) : IIntValue
{
    public int X = x;

    [UnscopedRef,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref int Ref() => ref X;

    public static implicit operator Int1(int v) => new(v);
    public static explicit operator int(Int1 v) => v.X;

    public static int Components => 1;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawInput(byte* label, int* value) =>
        ImGui.InputInt(label, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawSlider(byte* label, int* value, int min, int max) =>
        ImGui.SliderInt(label, value, min, max);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawDrag(byte* label, int* value, float speed, int min, int max) =>
        ImGui.DragInt(label, value, speed, min, max);

}

[StructLayout(LayoutKind.Sequential)]
public struct Int2(int x, int y) : IIntValue
{
    public int X = x, Y = y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Int2(Vector2I v) => new(v.X, v.Y);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Int2(Size2D v) => new(v.Width, v.Height);
    
    public static explicit operator Vector2I(Int2 v) => new(v.X, v.Y);
    public static explicit operator Size2D(Int2 v) => new(v.X, v.Y);


    [UnscopedRef,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref int Ref() => ref X;

    public static int Components => 2;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawInput(byte* label, int* value) =>
        ImGui.InputInt2(label, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawSlider(byte* label, int* value, int min, int max) =>
        ImGui.SliderInt2(label, value, min, max);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawDrag(byte* label, int* value, float speed, int min, int max) =>
        ImGui.DragInt2(label, value, speed, min, max);

}

[StructLayout(LayoutKind.Sequential)]
public struct Int3(int x, int y, int z) : IIntValue
{
    public int X = x, Y = y, Z = z;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Int3(Vector3I v) => new(v.X, v.Y,  v.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Int3(Size3D v) => new(v.Width, v.Height,  v.Depth);


    [UnscopedRef,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref int Ref() => ref X;

    public static int Components => 3;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawInput(byte* label, int* value) =>
        ImGui.InputInt3(label, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawSlider(byte* label, int* value, int min, int max) =>
        ImGui.SliderInt3(label, value, min, max);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawDrag(byte* label, int* value, float speed, int min, int max) =>
        ImGui.DragInt3(label, value, speed, min, max);

}

[StructLayout(LayoutKind.Sequential)]
public struct Int4(int x, int y, int z, int w) : IIntValue
{
    public int X = x, Y = y, Z = z, W = w;

    [UnscopedRef,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref int Ref() => ref X;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void From<T>(T value) where T : IIntValue { Unsafe.As<Int4, T>(ref this) = value;}

    public static int Components => 3;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawInput(byte* label, int* value) =>
        ImGui.InputInt4(label, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawSlider(byte* label, int* value, int min, int max) =>
        ImGui.SliderInt4(label, value, min, max);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool DrawDrag(byte* label, int* value, float speed, int min, int max) =>
        ImGui.DragInt4(label, value, speed, min, max);

}