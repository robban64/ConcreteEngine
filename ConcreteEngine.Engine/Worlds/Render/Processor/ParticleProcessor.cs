using System.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.MeshGeneration;
using ConcreteEngine.Engine.Worlds.MeshGeneration.MeshData;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class ParticleProcessor
{
    internal static void Execute(WorldParticles worldParticles)
    {
        var alpha = worldParticles.ParticleAlpha;
        var dt = worldParticles.ParticleDelta;
        
        ParticleDefinition def = default;
        int prevEmitterHandle = -1;

        foreach (var query in WorldEntities.Query<ParticleComponent>())
        {
            ref var entity = ref DrawEntityStore.GetEntityById(query.Entity);
            ref var component = ref query.Component;

            var emitter = worldParticles.GetEmitter(component.Emitter);
            
            entity.Meta.Queue = DrawCommandQueue.Particles;
            entity.Meta.PassMask = PassMask.Main;
            entity.Meta.CommandId = DrawCommandId.Particle;
            entity.Source.InstanceCount = emitter.ParticleCount;
            entity.Source.Model = new ModelId(emitter.MeshId);
            entity.Source.MaterialKey = new MaterialTagKey(emitter.MaterialId);

            if (prevEmitterHandle != emitter.EmitterHandle)
            {
                def = emitter.Definition;
                prevEmitterHandle = emitter.EmitterHandle;
            }
            
            var writer = worldParticles.GetMeshWriterFor(emitter);
            ProcessAll(writer, emitter.ParticlesSpan, def, dt, alpha);

        }
    }

    private static void ProcessAll(ParticleMeshWriter writer, ReadOnlySpan<ParticleStateData> particles, ParticleDefinition def, float dt, float alpha)
    {
        var gpuParticles = writer.Particles;
        var len = particles.Length;

        if ((uint)len > particles.Length || (uint)len > gpuParticles.Length)
            throw new IndexOutOfRangeException();

        for (var i = 0; i < len; i++)
        {
            ref readonly var particle = ref particles[i];
            ref var gpuData = ref gpuParticles[i];
            Process(ref gpuData, in particle, in def, dt, alpha);
        }

        writer.UploadGpuData();

    }
    
    private static void Process(ref ParticleInstanceData gpuData, in ParticleStateData particle, in ParticleDefinition def, float fixedDt, float alpha)
    {
        const float peakAlpha = 1.0f;
        var t = 1f - particle.Life / particle.MaxLife;

        var timeOffset = fixedDt * alpha; 
        var newPos = particle.Position + particle.Velocity * timeOffset;
            
        var fadeFactor = 4.0f * t * (1.0f - t);
        gpuData.Color = Vector4.Lerp(def.StartColor, def.EndColor, t);
        gpuData.Color.W *= peakAlpha * fadeFactor;
            
        var newSize = float.Lerp(def.SizeStartEnd.X, def.SizeStartEnd.Y, t);
        gpuData.PositionSize = new Vector4(newPos, newSize);

    }
}