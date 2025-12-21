using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class RenderEntityCollector
{
    public static RenderEntityId CollectEntities(in DrawEntityContext ctx, RenderEntityCore coreEcs)
    {
        var len = ctx.EntitySpan.Length;
        var highEntityId = 0;

        var ecsSourceSpan = coreEcs.GetSourceSpan();
        for (var i = 0; i < len; i++)
        {
            var entityId = ctx.EntityIndices[i];
            ref var drawEntity = ref ctx.EntitySpan[i];
            ref readonly var source = ref ecsSourceSpan[entityId.Index()];
            
            drawEntity.RenderEntity = entityId;
            drawEntity.Source = new DrawEntitySource(source.Model, source.MaterialKey);
            drawEntity.Meta = new DrawEntityMeta(DrawCommandId.Model, DrawCommandQueue.Opaque, PassMask.Default);
            
            highEntityId = int.Max(highEntityId, entityId);
        }

        return new RenderEntityId(highEntityId);
    }
    
    public static void UploadDrawCommands(RenderContext renderCtx, in DrawEntityContext ctx, in DrawCommandUploader uploader)
    {
        var matTable = renderCtx.MaterialTable;
        var meshTable = renderCtx.MeshTable;

        MaterialTag materialTag = default;
        ModelId prevModel = default;
        MaterialTagKey prevMatKey = default;

        var parts = ReadOnlySpan<MeshPart>.Empty;

        foreach (var it in ctx)
        {
            ref var entity = ref it.DrawEntity;
            var source = entity.Source;
            var baseMeta = entity.Meta;

            if (source.MaterialKey != prevMatKey)
            {
                materialTag = matTable.GetMaterialTag(source.MaterialKey);
                prevMatKey = source.MaterialKey;
            }

            if (prevModel != source.Model)
            {
                parts = meshTable.GetMeshParts(source.Model);
                prevModel = source.Model;
            }

            foreach (var part in parts)
            {
                ProcessParts(baseMeta, source, in materialTag, part, out var cmd, out var meta);
                uploader.SubmitDraw(in cmd, in meta);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessParts(DrawEntityMeta baseMeta, DrawEntitySource source, in MaterialTag materialTag,
        MeshPart part, out DrawCommand cmd, out DrawCommandMeta meta)
    {
        var isTransparent = materialTag.ResolveSlot(part.MaterialSlot, out var materialId);

        if (!isTransparent) meta = Unsafe.As<DrawEntityMeta, DrawCommandMeta>(ref baseMeta);
        else
        {
            var depthKey = (ushort)(ushort.MaxValue - baseMeta.DepthKey);
            var queue = (DrawCommandQueue)byte.Max((byte)baseMeta.Queue, (byte)DrawCommandQueue.Transparent);
            meta = new DrawCommandMeta(baseMeta.CommandId, queue, baseMeta.PassMask, depthKey);
        }

        cmd = new DrawCommand(part.Mesh, materialId, source.InstanceCount, source.AnimatedSlot, source.Resolver);
    }
}