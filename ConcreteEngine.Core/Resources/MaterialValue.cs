#region

using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Resources;

public interface IMaterialValue : IEquatable<IMaterialValue>
{
    UniformValueKind Kind { get; }
}

public readonly struct MaterialValue<T> : IMaterialValue
    where T : struct, IEquatable<T>
{
    public MaterialValue(T value, UniformValueKind kind)
    {
        Value = value;
        Kind = kind;
    }

    public MaterialValue(in T value, UniformValueKind kind)
    {
        Value = value;
        Kind = kind;
    }

    public T Value { get; }
    public UniformValueKind Kind { get; }

    public bool Equals(IMaterialValue? other) => other is MaterialValue<T> tOther && Value.Equals(tOther.Value);
}