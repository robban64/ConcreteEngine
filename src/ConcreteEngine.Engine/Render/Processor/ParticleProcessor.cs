using System.Numerics;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Render.Processor;

internal static class ParticleProcessor
{
    internal static void TagParticles(in DrawEntityContext ctx, ParticleManager particleManager)
    {
        foreach (var query in Ecs.Render.Query<ParticleComponent>())
        {
            var drawItem = ctx.TryGetVisible(query.Entity);
            if (drawItem.Entity == 0) continue;

            var component = query.Component;
            var emitter = particleManager.GetEmitter(component.Emitter);
            drawItem.Command.InstanceCount = emitter.ParticleCount;
            drawItem.Command.MeshId = emitter.Mesh;
            drawItem.Command.MaterialId = component.Material;
            drawItem.Meta = new DrawCommandMeta(DrawCommandId.Particle, DrawCommandQueue.Particles, PassMask.Main);
        }
    }

    internal static void Execute(ParticleManager particleManager)
    {
        var timeOffset = EngineTime.EnvironmentDelta * EngineTime.EnvironmentAlpha;
        ParticleEmitter? prevEmitter = null;
        ParticleMeshWriter writer = default;
        ParticleDefinition definition = default;

        foreach (var query in Ecs.Render.Query<ParticleComponent>())
        {
            if (!Ecs.Render.Core.IsVisible(query.Entity)) continue;
            var component = query.Component;

            if (prevEmitter?.EmitterHandle != component.Emitter)
            {
                var emitter = particleManager.GetEmitter(component.Emitter);
                writer = particleManager.GetMeshWriterFor(emitter);
                definition = emitter.Definition;
            }

            ProcessEmitter(writer, in definition, timeOffset);
        }
    }

    private static void ProcessEmitter(ParticleMeshWriter writer, in ParticleDefinition def, float timeOffset)
    {
        var len = writer.ParticleCount;
        if ((uint)len > (uint)writer.ParticleSpan.Length || (uint)len > (uint)writer.GpuParticleSpan.Length)
            throw new IndexOutOfRangeException();

        for (var i = 0; i < len; i++)
        {
            ref readonly var p = ref writer.ParticleSpan[i];
            ref var gpuData = ref writer.GpuParticleSpan[i];

            var t = 1f - p.Life / p.MaxLife;

            var newSize = float.Lerp(def.SizeStartEnd.X, def.SizeStartEnd.Y, t);
            gpuData.PositionSize = new Vector4(p.Position + p.Velocity * timeOffset, newSize);

            const float peakAlpha = 1.0f;
            var fadeFactor = 4.0f * t * (1.0f - t) * peakAlpha;
            gpuData.Color = Vector4.Lerp(def.StartColor, def.EndColor, t) with { W = fadeFactor };
        }

        writer.UploadGpuData();
    }
}