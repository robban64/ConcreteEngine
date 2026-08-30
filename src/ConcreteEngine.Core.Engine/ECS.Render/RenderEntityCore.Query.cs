using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using static ConcreteEngine.Core.Engine.ECS.Render.Queries.RenderCoreQuery;

namespace ConcreteEngine.Core.Engine.ECS.Render;

public sealed partial class RenderEntityCore
{
    public CullQueryEnumerator CullQuery(EntityDrawStatus status) =>
        new(PolicyView(), VisibilityView(), WorldBoundView(), status);

    public VisibilityQueryEnumerator<BoundingAxisBox> VisibilityBoundsQuery(PassMask passes) =>
        new(VisibilityView(), PolicyView(), WorldBoundView(), passes);
    
}