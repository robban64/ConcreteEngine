using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.RenderComponent;

public struct SkinLinkComponent : IRenderComponent<SkinLinkComponent>
{
    public RenderEntityId EntityId;
}

[StructLayout(LayoutKind.Sequential)]
public struct SkinningComponent(Id16<ModelAnimation> animationId, ushort instance)
    : IRenderComponent<SkinningComponent>, IEquatable<SkinningComponent>
{
    public float Time;
    public short Clip;

    public Id16<ModelAnimation> AnimationId = animationId;
    public ushort Instance = instance;

    public ushort AnimationSlot;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(SkinningComponent a, SkinningComponent b) => a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(SkinningComponent a, SkinningComponent b) => !a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(SkinningComponent other) =>
        AnimationId == other.AnimationId && Instance == other.Instance;

    public override readonly bool Equals(object? obj) => obj is SkinningComponent other && Equals(other);

    public override readonly int GetHashCode() => HashCode.Combine(AnimationId.Value, Instance);
}