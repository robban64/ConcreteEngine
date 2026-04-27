using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Editor.Lib.Field;

internal interface IFieldValue
{
    static abstract int Components { get; }
}

internal interface IFloatValue : IFieldValue
{
    [UnscopedRef]
    ref float GetRef();
}

internal interface IIntValue : IFieldValue
{
    [UnscopedRef]
    ref int GetRef();
}

[StructLayout(LayoutKind.Sequential)]
internal struct Float1(float x) : IFloatValue
{
    public float X = x;

    [UnscopedRef]
    public ref float GetRef() => ref X;

    public static implicit operator Float1(float v) => new(v);
    public static explicit operator float(Float1 v) => v.X;

    public static int Components => 1;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Float2(float x, float y) : IFloatValue
{
    public float X = x, Y = y;

    [UnscopedRef]
    public ref float GetRef() => ref X;

    public static implicit operator Float2(Vector2 v) => new(v.X, v.Y);
    public static explicit operator Vector2(Float2 v) => new(v.X, v.Y);

    public static int Components => 2;
}

[StructLayout(LayoutKind.Sequential)]
public struct Float3(float x, float y, float z) : IFloatValue
{
    public float X = x, Y = y, Z = z;

    [UnscopedRef]
    public ref float GetRef() => ref X;

    public static implicit operator Float3(Vector3 v) => new(v.X, v.Y, v.Z);
    public static explicit operator Vector3(Float3 v) => new(v.X, v.Y, v.Z);

    public static int Components => 3;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Float4(float x, float y, float z, float w = 0f) : IFloatValue
{
    public float X = x, Y = y, Z = z, W = w;

    [UnscopedRef]
    public ref float GetRef() => ref X;

    public static implicit operator Float4(Vector4 v) => new(v.X, v.Y, v.Z, v.W);
    public static implicit operator Float4(Color4 v) => new(v.R, v.G, v.B, v.A);

    public static explicit operator Color4(Float4 v) => new(v.X, v.Y, v.Z, v.W);
    public static explicit operator Vector4(Float4 v) => new(v.X, v.Y, v.Z, v.W);
    public static explicit operator Vector3(Float4 v) => new(v.X, v.Y, v.Z);

    public static int Components => 4;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Int1(int x) : IIntValue
{
    public int X = x;

    [UnscopedRef]
    public ref int GetRef() => ref X;

    public static implicit operator Int1(int v) => new(v);
    public static explicit operator int(Int1 v) => v.X;

    public static int Components => 1;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Int2(int x, int y) : IIntValue
{
    public int X = x, Y = y;

    [UnscopedRef]
    public ref int GetRef() => ref X;

    public static int Components => 2;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Int3Value(int x, int y, int z) : IIntValue
{
    public int X = x, Y = y, Z = z;

    [UnscopedRef]
    public ref int GetRef() => ref X;

    public static int Components => 3;
}