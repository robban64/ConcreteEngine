using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Engine.ECS.Data;

internal readonly ref struct RenderEntityView(
    RenderEntityId renderEntityId,
    ref SourceComponent source,
    ref Transform transform,
    ref BoxComponent box)
{
    public readonly RenderEntityId RenderEntityId = renderEntityId;
    public readonly ref SourceComponent Source = ref source;
    public readonly ref Transform Transform = ref transform;
    public readonly ref BoxComponent Box = ref box;
}

internal readonly ref struct RenderEntityContext(
    int count,
    Span<SourceComponent> sources,
    Span<Transform> transforms,
    Span<BoxComponent> boxes)
{
    public readonly Span<SourceComponent> Sources = sources;
    public readonly Span<Transform> Transforms = transforms;
    public readonly Span<BoxComponent> Boxes = boxes;

    public readonly int Count = count;

    public RenderEntityView GetEntityView(RenderEntityId e)
    {
        var idx = e.Index;
        if ((uint)idx >= Sources.Length) throw new IndexOutOfRangeException();
        return new RenderEntityView(e, ref Sources[idx], ref Transforms[idx], ref Boxes[idx]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref SourceComponent GetSource(RenderEntityId renderEntity) => ref Sources[renderEntity.Index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Transform GetTransform(RenderEntityId renderEntity) => ref Transforms[renderEntity.Index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref BoxComponent GetBox(RenderEntityId renderEntity) => ref Boxes[renderEntity.Index];
}