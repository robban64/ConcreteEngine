using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Models.Loader;

namespace ConcreteEngine.Engine.Assets.Models.Importer;

internal static class ModelImportUtils
{
    
    public static void CalculateBoundingBox(int count, Span<MeshPartImportResult> parts, out BoundingBox bounds)
    {
        bounds = parts[0].Bounds;
        for (var i = 1; i < count; i++)
            BoundingBox.Merge(in bounds, in parts[i].Bounds, out bounds);
    }

    // cm->0.01f, mm->0.001f, m->1f
    public static float DecideScale(in BoundingBox bounds, float unitScale)
    {
        var size = bounds.Max - bounds.Min;
        var maxDim = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        return unitScale * (maxDim > 100f ? 0.01f : maxDim < 0.01f ? 0.001f : 1f);
    }
}