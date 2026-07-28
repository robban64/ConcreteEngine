using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Engine.ECS;

[StructLayout(LayoutKind.Sequential)]
public struct RenderEntityMeta
{
    public bool Alive;
    public EntityVisibility Visibility;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsVisible() => Alive && Visibility == 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityVisibility ToggleVisibility(EntityVisibility flag, bool isVisible)
    {
        if (isVisible) Visibility &= ~flag;
        else Visibility |= flag;
        return Visibility;
    }
}

public readonly record struct RenderEntityId(int Id) : IComparable<RenderEntityId>
{
    public readonly int Id = Id;

    public bool IsValid() => Id > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Id - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(RenderEntityId other) => Id.CompareTo(other.Id);

    public static explicit operator int(RenderEntityId e) => e.Id;
}

public readonly record struct GameEntityId(int Id) : IComparable<GameEntityId>
{
    public readonly int Id = Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Id > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Id - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(GameEntityId other) => Id.CompareTo(other.Id);

    public static explicit operator int(GameEntityId e) => e.Id;
}