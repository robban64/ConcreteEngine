#region

using System.Numerics;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Objects;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawParticleProcessor
{
    private static readonly List<ParticleEmitter> Emitters = [];

    internal static void Execute(DrawEntityContext ctx, WorldParticles worldParticles)
    {
        Emitters.Clear();
        Emitters.EnsureCapacity(DrawDataProvider.WorldEntities.Particles.Count);
        
        foreach (var query in DrawDataProvider.WorldEntities.Query<ParticleComponent>())
        {
            ref var drawEntity = ref ctx.GetByEntityId(query.Entity);
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

        foreach (var emitter in Emitters)
            ProcessEmitter(worldParticles, emitter);
    }

    private static void ProcessEmitter(WorldParticles worldParticles, ParticleEmitter emitter)
    {
        var writer = worldParticles.GetMeshWriterFor(emitter);

        var particles = emitter.Particles;
        var gpuParticles = writer.Particles;
        var len = particles.Length;
        var def = emitter.Definition;

        float timeOffset = EngineTime.SimulationDeltaTime * EngineTime.SimulationAlpha;

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
            gpuData.Color = Vector4.Lerp(def.StartColor, def.EndColor, t) with { W = fadeFactor };
        }

        writer.UploadGpuData();
    }
}