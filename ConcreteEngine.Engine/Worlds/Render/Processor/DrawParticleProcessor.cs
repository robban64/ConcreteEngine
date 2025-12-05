using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.MeshGeneration;
using ConcreteEngine.Engine.Worlds.MeshGeneration.MeshData;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawParticleProcessor
{
    internal static void Execute(DrawEntityContext ctx,WorldParticles worldParticles)
    {
        foreach (var query in DrawDataProvider.WorldEntities.Query<ParticleComponent>())
        {
            ref var entity = ref ctx.GetByEntityId(query.Entity);
            ref var component = ref query.Component;

            var emitter = worldParticles.GetEmitter(component.EmitterHandle);

            entity.Meta.Queue = DrawCommandQueue.Particles;
            entity.Meta.PassMask = PassMask.Main;
            entity.Meta.CommandId = DrawCommandId.Particle;
            entity.Source.InstanceCount = emitter.ParticleCount;
            entity.Source.Model = new ModelId(emitter.MeshId);
            entity.Source.MaterialKey = new MaterialTagKey(emitter.MaterialId);

            ProcessEmitter(worldParticles, emitter);
        }
    }

    private static void ProcessEmitter(WorldParticles worldParticles, ParticleEmitter emitter)
    {
        var writer = worldParticles.GetMeshWriterFor(emitter);

        var particles = emitter.Particles;
        var gpuParticles = writer.Particles;
        var len = particles.Length;
        var def = emitter.Definition;

        float timeOffset = worldParticles.ParticleDelta * worldParticles.ParticleAlpha;

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