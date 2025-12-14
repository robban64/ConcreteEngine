using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawEntityUploader
{
    public static void UploadDrawCommands(World world, in DrawEntityContext ctx, in DrawCommandUploader uploader)
    {
        var matTable = world.MaterialTableImpl;
        var meshTable = world.MeshTableImpl;

        MaterialTag materialTag = default;
        var prevMatKey = new MaterialTagKey(-1);
        foreach (var it in ctx)
        {
            ref readonly var entity = ref it.DrawEntity;
            var matKey = entity.Source.MaterialKey;
            if (matKey != prevMatKey)
            {
                matTable.ResolveSubmitMaterial(matKey, out materialTag);
                prevMatKey = matKey;
            }

            var parts = meshTable.GetMeshParts(entity.Source.Model);
            foreach (ref readonly var part in parts)
            {
                var isTransparent = materialTag.ResolveSlot(part.MaterialSlot, out var materialId);
                var cmd = new DrawCommand(part.Mesh, materialId, part.DrawCount, entity.Source.InstanceCount);
                uploader.SubmitDraw(in cmd, BuildMeta(entity.Meta, isTransparent));
            }
        }

    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DrawCommandMeta BuildMeta(DrawEntityMeta m, bool isTransparent)
    {
        if (!isTransparent) return m.ToCommandMeta();

        var depthKey = (ushort)(ushort.MaxValue - m.DepthKey);
        var queue = m.Queue >= DrawCommandQueue.Transparent ? m.Queue : DrawCommandQueue.Transparent;
        return new DrawCommandMeta(m.CommandId, queue, m.Resolver, m.PassMask, depthKey, m.AnimatedSlot);
    }

    private static void ExecuteGeneratedCommand(in DrawEntity entity, DrawCommandUploader uploader)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(entity.Source.MaterialKey.Value, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(entity.Source.Model, 1);

        var mesh = new MeshId(entity.Source.Model);
        var material = new MaterialId(entity.Source.MaterialKey.Value);
        var cmd = new DrawCommand(mesh, material, entity.Source.DrawCount, entity.Source.InstanceCount);
        uploader.SubmitDrawIdentity(cmd, entity.Meta.ToCommandMeta());
    }
}