using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Editor.Lib;

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
internal struct Float1Value(float x) : IFloatValue
{
    public float X = x;

    [UnscopedRef]
    public ref float GetRef() => ref X;

    public static implicit operator Float1Value(float v) => new(v);
    public static explicit operator float(Float1Value v) => v.X;

    public static int Components => 1;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Float2Value(float x, float y) : IFloatValue
{
    public float X = x, Y = y;

    [UnscopedRef]
    public ref float GetRef() => ref X;

    public static implicit operator Float2Value(Vector2 v) => new(v.X, v.Y);
    public static explicit operator Vector2(Float2Value v) => new(v.X, v.Y);

    public static int Components => 2;
}

[StructLayout(LayoutKind.Sequential)]
public struct Float3Value(float x, float y, float z) : IFloatValue
{
    public float X = x, Y = y, Z = z;

    [UnscopedRef]
    public ref float GetRef() => ref X;

    public static implicit operator Float3Value(Vector3 v) => new(v.X, v.Y, v.Z);
    public static explicit operator Vector3(Float3Value v) => new(v.X, v.Y, v.Z);

    public static int Components => 3;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Float4Value(float x, float y, float z, float w) : IFloatValue
{
    public float X = x, Y = y, Z = z, W = w;

    [UnscopedRef]
    public ref float GetRef() => ref X;

    [UnscopedRef]
    public ref float GetRef(int i) => ref Unsafe.Add(ref X, i);

    public static implicit operator Float4Value(Vector4 v) => new(v.X, v.Y, v.Z, v.W);
    public static implicit operator Float4Value(Color4 v) => new(v.R, v.G, v.B, v.A);

    public static explicit operator Color4(Float4Value v) => new(v.X, v.Y, v.Z, v.W);
    public static explicit operator Vector4(Float4Value v) => new(v.X, v.Y, v.Z, v.W);
    public static explicit operator Vector3(Float4Value v) => new(v.X, v.Y, v.Z);

    public static int Components => 4;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Int1Value(int x) : IIntValue
{
    public int X = x;

    [UnscopedRef]
    public ref int GetRef() => ref X;

    public static implicit operator Int1Value(int v) => new(v);
    public static explicit operator int(Int1Value v) => v.X;

    public static int Components => 1;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Int2Value(int x, int y) : IIntValue
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