using System.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Mesh;

namespace ConcreteEngine.Engine.Render.Processor;

internal static class ParticleProcessor
{
    internal static void TagParticles(in DrawEntityContext ctx, ParticleSystem particleSystem)
    {
        foreach (var query in Ecs.Render.Query<ParticleComponent>())
        {
            var drawPtr = ctx.TryGetVisible(query.RenderEntity);
            if (drawPtr.IsNull) continue;

            var component = query.Component;
            var emitter = particleSystem.GetEmitter(component.Emitter);
            drawPtr.Value.Meta = new DrawEntityMeta(DrawCommandId.Particle, DrawCommandQueue.Particles, PassMask.Main);
            drawPtr.Value.Source.InstanceCount = emitter.ParticleCount;
            drawPtr.Value.Source.Mesh = emitter.Mesh;
            drawPtr.Value.Source.Material = component.Material;
        }
    }

    internal static void Execute(in DrawEntityContext ctx, ParticleSystem particleSystem)
    {
        var timeOffset = EngineTime.EnvironmentDelta * EngineTime.EnvironmentAlpha;
        ParticleEmitter? prevEmitter = null;
        ParticleMeshWriter writer = default;
        ParticleDefinition definition = default;

        foreach (var query in Ecs.Render.Query<ParticleComponent>())
        {
            var index = ctx.ByEntityIdSpan[query.RenderEntity];
            if (index == -1) continue;
            var component = query.Component;

            if (prevEmitter?.EmitterHandle != component.Emitter)
            {
                var emitter = particleSystem.GetEmitter(component.Emitter);
                writer = particleSystem.GetMeshWriterFor(emitter);
                definition = emitter.Definition;
            }

            ProcessEmitter(writer, in definition, timeOffset);
        }
    }

    private static void ProcessEmitter(ParticleMeshWriter writer, in ParticleDefinition def, float timeOffset)
    {
        var gpuParticles = writer.GpuParticleSpan;
        var particles = writer.ParticleSpan;

        var len = writer.ParticleCount;
        if ((uint)len > particles.Length || (uint)len > gpuParticles.Length)
            throw new IndexOutOfRangeException();

        for (var i = 0; i < len; i++)
        {
            ref readonly var p = ref particles[i];
            ref var gpuData = ref gpuParticles[i];

            var t = 1f - p.Life / p.MaxLife;

            var newSize = float.Lerp(def.SizeStartEnd.X, def.SizeStartEnd.Y, t);
            gpuData.PositionSize = new Vector4(p.Position + p.Velocity * timeOffset, newSize);

            const float peakAlpha = 1.0f;
            var fadeFactor = 4.0f * t * (1.0f - t) * peakAlpha;
            gpuData.Color =
                Vector4.Lerp(def.StartColor, def.EndColor, t) with { W = fadeFactor };
        }

        writer.UploadGpuData();
    }
}