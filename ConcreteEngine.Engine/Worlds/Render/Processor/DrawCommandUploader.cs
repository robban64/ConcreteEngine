using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawCommandUploader
{
    public static void UploadDrawCommands(DrawEntityContext ctx)
    {
        var entities = ctx.EntitySpan;

        var len = entities.Length;

        MaterialTag materialTag = default;
        var prevMatKey = new MaterialTagKey(-1);

        for (var i = 0; i < len; i++)
        {
            ref readonly var entity = ref entities[i];
            if (entity.Meta.CommandId != DrawCommandId.Model)
            {
                ExecuteGeneratedCommand(in entity);
                continue;
            }

            var matKey = entity.Source.MaterialKey;
            if (matKey != prevMatKey)
            {
                DrawDataProvider.ResolveMaterial(matKey, out materialTag);
                prevMatKey = matKey;
            }

            ExecuteSubmitCommand(in entity, in materialTag);
        }
    }


    private static void ExecuteGeneratedCommand(in DrawEntity entity)
    {
        var writer = DrawDataProvider.GetDrawUploaderCtx();
        var mesh = new MeshId(entity.Source.Model);
        var material = new MaterialId(entity.Source.MaterialKey.Value);
        var cmd = new DrawCommand(mesh, material, entity.Source.DrawCount, entity.Source.InstanceCount);
        writer.SubmitDrawIdentity(cmd, entity.Meta.ToCommandMeta());
    }


    private static void ExecuteSubmitCommand(in DrawEntity entity, in MaterialTag materialTag)
    {
        var parts = DrawDataProvider.GetMeshParts(entity.Source.Model);
        var writer = DrawDataProvider.GetDrawUploaderCtx();

        foreach (ref readonly var part in parts)
        {
            var isTransparent = materialTag.ResolveSlot(part.MaterialSlot, out var materialId);
            var cmd = new DrawCommand(part.Mesh, materialId, part.DrawCount, entity.Source.InstanceCount);

            var meta = BuildMeta(entity.Meta, isTransparent);

            writer.SubmitDraw(in cmd, meta);
        }

        return;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DrawCommandMeta BuildMeta(DrawEntityMeta m, bool isTransparent)
        {
            if (!isTransparent) return m.ToCommandMeta();

            var depthKey = (ushort)(ushort.MaxValue - m.DepthKey);
            var queue = m.Queue >= DrawCommandQueue.Transparent ? m.Queue : DrawCommandQueue.Transparent;
            return new DrawCommandMeta(m.CommandId, queue, m.Resolver, m.PassMask, depthKey, m.AnimatedSlot);
        }
    }
}