using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;

namespace ConcreteEngine.Core.Engine.Scene;
/*
public readonly record struct SceneObjectId(int Value, ushort Gen) : IComparable<SceneObjectId>
{
    public SceneObjectId(int id, int gen) : this(id, (ushort)gen) { }
    public readonly int Value = Value;
    public readonly ushort Gen = Gen;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Value > 0 && Gen > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Value - 1;

    public static implicit operator Handle32<SceneObject>(SceneObjectId handle) => new(handle.Value, handle.Gen);
    public static explicit operator SceneObjectId(Handle32<SceneObject> handle) => new(handle.Value, handle.Gen);
    public static explicit operator int(SceneObjectId handle) => handle.Value;

    public int CompareTo(SceneObjectId other) => Value.CompareTo(other.Value);

    public static SceneObjectId Empty = new(0, 0);
}*/