using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using static ConcreteEngine.Core.Engine.ECS.Render.Queries.RenderCoreQuery;

namespace ConcreteEngine.Core.Engine.ECS.Render;

public sealed partial class RenderEntityCore
{
    /*
    [SkipLocalsInit]
    public CullQueryEnumerator CullQuery(EntityDrawStatus status) =>
        new(PolicyView(), VisibilityView(), WorldBoundView(), status);
    */

    [SkipLocalsInit]
    public VisibilityQueryEnumerator<BoundingAxisBox> VisibilityBoundsQuery() =>
        new(VisibleSet, _entityDataStore.PolicyView(Count), _entityDataStore.WorldBoundView(Count));

    [SkipLocalsInit]
    public VisibilityQueryEnumerator<TransformUniform> VisibilityTransformQuery() =>
        new(VisibleSet, _entityDataStore.PolicyView(Count), TransformView());
}