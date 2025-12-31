using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.Scene;

public readonly record struct SceneObjectId(int Id, ushort Gen) : IComparable<SceneObjectId>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Id > 0 && Gen > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Id - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(SceneObjectId other) => Id.CompareTo(other.Id);

    public static implicit operator int(SceneObjectId handle) => handle.Id;
}