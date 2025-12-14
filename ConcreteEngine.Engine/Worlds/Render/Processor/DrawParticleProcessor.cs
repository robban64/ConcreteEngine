#region

using System.Numerics;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.MeshGeneration;
using ConcreteEngine.Engine.Worlds.Objects;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Shared.World;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawParticleProcessor
{
    private static readonly List<ParticleEmitter> Emitters = new(8);

    internal static void TagParticles(DrawEntityContext ctx, WorldParticles worldParticles)
    {
        Emitters.Clear();
        Emitters.EnsureCapacity(DrawDataProvider.WorldEntities.Particles.Count);

        foreach (var query in DrawDataProvider.WorldEntities.Query<ParticleComponent>())
        {
            var index = ctx.ByEntityIdSpan[query.Entity];
            if (index == -1) continue;
            ref var drawEntity = ref ctx.EntitySpan[index];

            var component = query.Component;
            var emitter = worldParticles.GetEmitter(component.EmitterHandle);

            drawEntity.Meta.Queue = DrawCommandQueue.Particles;
            drawEntity.Meta.PassMask = PassMask.Main;
            drawEntity.Meta.CommandId = DrawCommandId.Particle;
            drawEntity.Source.InstanceCount = emitter.ParticleCount;
            drawEntity.Source.Model = new ModelId(emitter.MeshId);
            drawEntity.Source.MaterialKey = new MaterialTagKey(emitter.MaterialId);

            Emitters.Add(emitter);
        }
    }

    internal static void Execute(WorldParticles worldParticles)
    {
        var timeOffset = EngineTime.SimulationDeltaTime * EngineTime.SimulationAlpha;
        var prevEmitterHandle = -1;
        ParticleMeshWriter writer = default;
        ParticleDefinition definition = default;
        foreach (var emitter in Emitters)
        {
            if (prevEmitterHandle != emitter.EmitterHandle)
            {
                writer = worldParticles.GetMeshWriterFor(emitter);
                definition = emitter.Definition;
            }

            ProcessEmitter(writer, in definition, timeOffset);

            prevEmitterHandle = emitter.EmitterHandle;
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