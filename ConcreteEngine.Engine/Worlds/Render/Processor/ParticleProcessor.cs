using System.Numerics;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Worlds.Mesh;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Shared.World;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class ParticleProcessor
{

    internal static void TagParticles(in DrawEntityContext ctx, ParticleSystem particleSystem)
    {
        foreach (var query in RenderQuery<ParticleComponent>.Query())
        {
            var drawPtr = ctx.TryGetVisible(query.RenderEntity);
            if (drawPtr.IsNull) continue;

            var component = query.Component;
            var emitter = particleSystem.GetEmitter(component.EmitterHandle);
            drawPtr.Value.Meta = new DrawEntityMeta(DrawCommandId.Particle, DrawCommandQueue.Particles, PassMask.Main);
            drawPtr.Value.Source.InstanceCount = emitter.ParticleCount;
            drawPtr.Value.Source.Model = emitter.Model;
            drawPtr.Value.Source.MaterialKey = emitter.MaterialKey;
        }
    }

    internal static void Execute(in DrawEntityContext ctx, ParticleSystem particleSystem)
    {
        var timeOffset = EngineTime.SimulationDeltaTime * EngineTime.SimulationAlpha;
        ParticleEmitter? prevEmitter = null;
        ParticleMeshWriter writer = default;
        ParticleDefinition definition = default;
        
        foreach (var query in RenderQuery<ParticleComponent>.Query())
        {
            var index = ctx.ByEntityIdSpan[query.RenderEntity];
            if (index == -1) continue;
            var component = query.Component;
            var emitter = particleSystem.GetEmitter(component.EmitterHandle);

            if (prevEmitter?.EmitterHandle != component.EmitterHandle)
            {
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