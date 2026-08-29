using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using static ConcreteEngine.Core.Engine.RenderEntity.Queries.RenderCoreQuery;

namespace ConcreteEngine.Core.Engine.RenderEntity;

public sealed partial class RenderEntityCore
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CullQueryEnumerator CullQuery(EntityDrawStatus status) =>
        new(GetDrawPolicyView(), GetVisibilityView(), GetWorldBoundView(), status);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibilityQueryEnumerator<BoundingAxisBox> VisibilityBoundsQuery(PassMask passes) =>
        new(GetVisibilityView(), GetWorldBoundView(), passes);
    
}