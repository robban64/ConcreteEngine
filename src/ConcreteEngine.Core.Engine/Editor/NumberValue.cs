using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Editor;

public interface INumberValue
{
    static abstract int Components { get; }
}

public interface IFloatValue : INumberValue
{
    [UnscopedRef]
    ref float Ref();
}

public interface IIntValue : INumberValue
{
    [UnscopedRef]
    ref int Ref();
}

[StructLayout(LayoutKind.Sequential)]
public struct Float1(float x) : IFloatValue
{
    public float X = x;

    [UnscopedRef,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref float Ref() => ref X;

    public static implicit operator Float1(float v) => new(v);
    public static explicit operator float(Float1 v) => v.X;

    public static int Components => 1;
}

[StructLayout(LayoutKind.Sequential)]
public struct Float2(float x, float y) : IFloatValue
{
    public float X = x, Y = y;

    [UnscopedRef,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref float Ref() => ref X;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Float2(Vector2 v) => new(v.X, v.Y);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Float2(YawPitch v) => new(v.Yaw, v.Pitch);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector2(Float2 v) => new(v.X, v.Y);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator YawPitch(Float2 v) => new(v.X, v.Y);

    public static int Components => 2;
}

[StructLayout(LayoutKind.Sequential)]
public struct Float3(float x, float y, float z) : IFloatValue
{
    public float X = x, Y = y, Z = z;

    [UnscopedRef,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref float Ref() => ref X;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Float3(Vector3 v) => new(v.X, v.Y, v.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector3(Float3 v) => new(v.X, v.Y, v.Z);

    public static int Components => 3;
}

[StructLayout(LayoutKind.Sequential)]
public struct Float4(float x, float y, float z, float w = 0f) : IFloatValue
{
    public float X = x, Y = y, Z = z, W = w;

    [UnscopedRef,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref float Ref() => ref X;

    [UnscopedRef,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnsafeFrom<T>(T value) where T : IFloatValue { Unsafe.As<Float4, T>(ref this) = value;}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Float4(in Vector4 v) => new(v.X, v.Y, v.Z, v.W);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Float4(in Color4 v) => new(v.R, v.G, v.B, v.A);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Color4(in Float4 v) => new(v.X, v.Y, v.Z, v.W);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector4(in Float4 v) => new(v.X, v.Y, v.Z, v.W);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector3(in Float4 v) => new(v.X, v.Y, v.Z);

    public static int Components => 4;
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
}