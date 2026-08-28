using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using static ConcreteEngine.Core.Engine.RenderEntity.Queries.RenderCoreQuery;

namespace ConcreteEngine.Core.Engine.RenderEntity;

public sealed unsafe partial class RenderEntityCore
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CullQueryEnumerator CullQuery(EntityDrawStatus status, PassMask passes) =>
        new(GetDrawPolicyView(), GetVisibilityView(), GetWorldBoundView(), status, passes);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibilityQueryEnumerator<BoundingAxisBox> VisibilityBoundsQuery(PassMask passes) =>
        new(GetVisibilityView(), GetWorldBoundView(), passes);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibilityQueryEnumerator<TransformUniform> VisibilityTransformQuery(PassMask passes) 
        => new(GetVisibilityView(), GetTransformView(), passes);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SparseQueryEnumerator<TransformUniform> SparseTransformQuery(NativeView<RenderEntityId> sparseEntities) 
        => new(sparseEntities, GetTransformView());

    //
}