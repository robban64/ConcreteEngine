using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Engine.ECS.Data;
/*
public readonly ref struct RenderEntityContext(
    int count,
    Span<SourceComponent> sources,
    Span<Transform> transforms,
    Span<BoundingBox> boxes,
    Span<Matrix4x4> parentMatrices)
{
    public readonly Span<SourceComponent> Sources = sources;
    public readonly Span<Transform> Transforms = transforms;
    public readonly Span<BoundingBox> Boxes = boxes;
    public readonly Span<Matrix4x4> ParentMatrices = parentMatrices;

    public readonly int Count = count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref SourceComponent GetSource(RenderEntityId renderEntity) => ref Sources[renderEntity.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Transform GetTransform(RenderEntityId renderEntity) => ref Transforms[renderEntity.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref BoundingBox GetBox(RenderEntityId renderEntity) => ref Boxes[renderEntity.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Matrix4x4 GetParentMatrix(RenderEntityId renderEntity) => ref ParentMatrices[renderEntity.Index()];
}*/