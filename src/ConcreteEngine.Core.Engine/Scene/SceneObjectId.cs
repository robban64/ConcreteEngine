using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Engine.Scene;

public readonly record struct SceneObjectId(int Id, ushort Gen) : IComparable<SceneObjectId>
{
    public readonly int Id = Id;
    public readonly ushort Gen = Gen;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Id > 0 && Gen > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Id - 1;

    public int CompareTo(SceneObjectId other) => Id.CompareTo(other.Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(SceneObjectId handle) => handle.Id;

    public static SceneObjectId Empty = new(0, 0);
}