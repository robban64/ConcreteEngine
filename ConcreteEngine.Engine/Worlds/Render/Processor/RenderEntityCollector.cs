using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Memory;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Draw;
using Ecs = ConcreteEngine.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class RenderEntityCollector
{
    public static RenderEntityId CollectEntities(in DrawEntityContext ctx)
    {
        var highEntityId = 0;

        var zip = ctx.GetZippedEntities();
        var len = zip.Length;
        for (var i = 0; i < len; i++)
        {
            var entityPtr = zip[i];
            var entityId = entityPtr.Item1;
            var sourcePtr = Ecs.Render.Core.TryGetSource(entityId);
            if (sourcePtr.IsNull) continue;

            ref readonly var source = ref sourcePtr.Value;
            ref var drawEntity = ref entityPtr.Item2;

            drawEntity.RenderEntity = entityId;
            drawEntity.Source = new DrawEntitySource(source.Model, source.MaterialKey);
            drawEntity.Meta = new DrawEntityMeta(DrawCommandId.Model, DrawCommandQueue.Opaque, PassMask.Default);

            highEntityId = int.Max(highEntityId, entityId);
        }

        return new RenderEntityId(highEntityId);
    }

    public static void UploadDrawCommands(RenderContext renderCtx, in DrawEntityContext ctx,
        in DrawCommandUploader uploader)
    {
        var matTable = renderCtx.MaterialTable;
        var meshTable = renderCtx.MeshTable;

        MaterialTag materialTag = default;
        ModelId prevModel = default;
        MaterialTagKey prevMatKey = default;

        var parts = ReadOnlySpan<MeshPart>.Empty;

        foreach (var it in ctx)
        {
            ref readonly var source = ref it.DrawEntity.Source;

            if (prevMatKey != source.MaterialKey)
            {
                materialTag = matTable.GetMaterialTag(source.MaterialKey);
                prevMatKey = source.MaterialKey;
            }

            if (prevModel != source.Model)
            {
                parts = meshTable.GetMeshParts(source.Model);
                prevModel = source.Model;
            }

            var baseMeta = it.DrawEntity.Meta;
            foreach (var part in parts)
            {
                var isTransparent = materialTag.ResolveSlot(part.MaterialSlot, out var materialId);
                var cmd = new DrawCommand(part.Mesh, materialId, source.InstanceCount,
                    source.AnimatedSlot, source.Resolver);

                var meta = ProcessParts(baseMeta, isTransparent);
                uploader.SubmitDraw(in cmd, meta);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DrawCommandMeta ProcessParts(DrawEntityMeta meta, bool isTransparent)
    {
        if (!isTransparent)
            return Unsafe.As<DrawEntityMeta, DrawCommandMeta>(ref meta);

        var depthKey = (ushort)(ushort.MaxValue - meta.DepthKey);
        var queue = (DrawCommandQueue)byte.Max((byte)meta.Queue, (byte)DrawCommandQueue.Transparent);
        return new DrawCommandMeta(meta.CommandId, queue, meta.PassMask, depthKey);
    }
}