using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using static ConcreteEngine.Core.Engine.ECS.Render.Queries.RenderCoreQuery;

namespace ConcreteEngine.Core.Engine.ECS.Render;

public sealed partial class RenderEntityCore
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CullQueryEnumerator CullQuery(EntityDrawStatus status) =>
        new(GetDrawPolicyView(), GetVisibilityView(), GetWorldBoundView(), status);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibilityQueryEnumerator<BoundingAxisBox> VisibilityBoundsQuery(PassMask passes) =>
        new(GetVisibilityView(), GetDrawPolicyView(), GetWorldBoundView(), passes);
    
}