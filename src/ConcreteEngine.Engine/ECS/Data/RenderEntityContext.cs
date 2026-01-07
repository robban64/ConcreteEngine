using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Engine.ECS.Data;

public readonly ref struct RenderEntityContext(
    int count,
    Span<SourceComponent> sources,
    Span<RenderTransform> transforms,
    Span<BoxComponent> boxes,
    Span<ParentMatrix> parentMatrices)
{
    public readonly Span<SourceComponent> Sources = sources;
    public readonly Span<RenderTransform> Transforms = transforms;
    public readonly Span<BoxComponent> Boxes = boxes;
    public readonly Span<ParentMatrix> ParentMatrices = parentMatrices;

    public readonly int Count = count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref SourceComponent GetSource(RenderEntityId renderEntity) => ref Sources[renderEntity.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref RenderTransform GetTransform(RenderEntityId renderEntity) => ref Transforms[renderEntity.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref BoxComponent GetBox(RenderEntityId renderEntity) => ref Boxes[renderEntity.Index()];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref ParentMatrix GetParentMatrix(RenderEntityId renderEntity) => ref ParentMatrices[renderEntity.Index()];

}