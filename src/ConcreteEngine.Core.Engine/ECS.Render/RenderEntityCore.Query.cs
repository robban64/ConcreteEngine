using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using static ConcreteEngine.Core.Engine.ECS.Render.Queries.RenderCoreQuery;

namespace ConcreteEngine.Core.Engine.ECS.Render;

public sealed partial class RenderEntityCore
{
    public VisibilityQueryEnumerator<BoundingAxisBox> VisibilityBoundsQuery() =>
        new(Data.VisibleSet, Data.Policies.Slice(0, Count), Data.WorldBounds.Slice(0, Count));
}