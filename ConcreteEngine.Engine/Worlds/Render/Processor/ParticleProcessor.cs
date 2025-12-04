using System.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.MeshGeneration;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class ParticleProcessor
{
    internal static void Execute(WorldParticles worldParticles)
    {
        var alpha = DrawDataProvider.FrameInfo.Alpha;

        foreach (var query in WorldEntities.Query<ParticleComponent>())
        {
            var emitter = worldParticles.GetEmitter(0);
            var writer = worldParticles.GetMeshWriterFor(emitter);
            ref var entity = ref DrawEntityStore.GetEntityById(query.Entity);
            entity.Meta.Queue = DrawCommandQueue.Particles;
            entity.Meta.PassMask = PassMask.Main;
            entity.Meta.CommandId = DrawCommandId.Particle;
            entity.Source.InstanceCount = emitter.ParticleCount;
            entity.Source.Model = new ModelId(emitter.MeshId);
            entity.Source.MaterialKey = new MaterialTagKey(emitter.MaterialId);
            Process(emitter, writer, alpha);
        }
    }

    private static void Process(ParticleEmitter emitter, ParticleMeshWriter writer, float alpha)
    {
        const float peakAlpha = 0.4f;

        var particles = emitter.Particles;
        var gpuParticles = writer.Particles;
        var def = emitter.Definition;

        var len = emitter.ParticleCount;
        if ((uint)len > particles.Length || (uint)len > gpuParticles.Length)
            throw new IndexOutOfRangeException();

        for (var i = 0; i < len; i++)
        {
            ref readonly var particle = ref particles[i];
            ref var gpuData = ref gpuParticles[i];

            var lifeRatio = 1f - (particle.Life / particle.MaxLife);

            var newPos = Vector3.Lerp(particle.PrevPosition, particle.Position, alpha);
            var newSize = float.Lerp(def.SizeStartEnd.X, def.SizeStartEnd.Y, lifeRatio);
            gpuData.PositionSize = new Vector4(newPos, newSize);

            float fadeCurve = 4.0f * lifeRatio * (1.0f - lifeRatio);
            gpuData.Color = Vector4.Lerp(def.StartColor, def.EndColor, lifeRatio);
            gpuData.Color.W = peakAlpha * fadeCurve;
        }

        writer.UploadGpuData(len);
    }
}