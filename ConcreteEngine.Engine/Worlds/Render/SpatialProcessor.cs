using ConcreteEngine.Engine.Worlds.Utility;

namespace ConcreteEngine.Engine.Worlds.Render;

internal static class SpatialProcessor
{
    internal static void Execute( DrawEntityContext ctx)
    {
        if (ctx.EntitySpan.Length == 0 || ctx.EntityDataSpan.Length == 0) return;
        
        var projInfo = RenderDataSlot.ProjectionInfo;
        var view = DepthKeyUtility.ExtractView(RenderDataSlot.ViewData.ViewMatrix);
        float near = projInfo.Near, far = projInfo.Far;

        var len = ctx.Count;

        if ((uint)len > ctx.EntitySpan.Length || (uint)len > ctx.EntityDataSpan.Length)
            throw new IndexOutOfRangeException();

        for (var i = 0; i < len; i++)
        {
            ref var entity = ref ctx.EntitySpan[i];
            ref readonly var translation = ref ctx.EntityDataSpan[i].Transform.Translation;
            var depthKey = DepthKeyUtility.MakeDepthKey(in view, in translation, near, far);
            entity.WithDepthKey(depthKey);
        }

    }
}