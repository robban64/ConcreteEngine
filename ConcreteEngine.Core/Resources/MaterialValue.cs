#region

using System.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Resources;

public interface IMaterialValue;

public static class MatValues
{
    public readonly record struct IntVal(int Value): IMaterialValue;
    public readonly record struct FloatVal(float Value): IMaterialValue;
    public readonly record struct Vec2Val(Vector2 Value): IMaterialValue;
    public readonly record struct Vec3Val(Vector3 Value): IMaterialValue;
    public readonly record struct Vec4Val(Vector4 Value): IMaterialValue;
    public readonly record struct Mat3Val(in Matrix3 Value): IMaterialValue;

    public readonly struct Mat4Val(in Matrix4x4 value) : IMaterialValue
    {
        public readonly Matrix4x4 Value = value;
    }
}

