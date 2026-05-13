using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Render.Processor;

internal static class ParticleProcessor
{
    private static readonly HashSet<Id32<ParticleEmitter>> ActiveEmitters = new(16);
    
    internal static void TagParticles(in DrawEntityContext ctx, ParticleManager particleManager)
    {
        ActiveEmitters.Clear();

        foreach (var query in Ecs.Render.Query<ParticleComponent>())
        {
            var drawItem = ctx.TryGetVisible(query.Entity);
            if (drawItem.Entity == 0) continue;

            var component = query.Component;
            var emitter = particleManager.GetEmitter(component.Emitter);
            drawItem.Command.InstanceCount = (uint)emitter.ParticleCount;
            drawItem.Command.MeshId = particleManager.GetEmitterMesh(emitter);
            drawItem.Command.MaterialId = component.Material;
            drawItem.Meta = new DrawCommandMeta(DrawCommandId.Particle, DrawCommandQueue.Particles, PassMask.Main);

            ActiveEmitters.Add(emitter.Id);
        }
    }
    
    internal static void Execute(ParticleManager particleManager)
    {
        var timeOffset = EngineTime.EnvironmentDelta * EngineTime.EnvironmentAlpha;

        foreach (var emitterHandle in ActiveEmitters)
        {
            var emitter = particleManager.GetEmitter(emitterHandle);
            var writer = particleManager.GetMeshWriterFor(emitter);
            ProcessEmitter(writer, timeOffset);
            particleManager.UploadWriter(writer);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessEmitter(ParticleMeshWriter writer, float timeOffset)
    {
        const float peakAlpha = 1.0f;
        
        ref readonly var visuals = ref writer.VisualParams;
        var len = writer.Length;
        for (var i = 0; i < len; i++)
        {
            ref readonly var p = ref writer.ParticleSpan[i];

            var t = 1f - p.Life / p.MaxLife;
            var newSize = float.Lerp(visuals.SizeStartEnd.X, visuals.SizeStartEnd.Y, t);
            var fadeFactor = 4.0f * t * (1.0f - t) * peakAlpha;

            ref var gpuData = ref writer.GpuParticleSpan[i];
            gpuData.PositionSize = new Vector4(p.Position + p.Velocity * timeOffset, newSize);
            gpuData.Color = Vector4.Lerp(visuals.StartColor, visuals.EndColor, t);
            gpuData.Color.W = fadeFactor;
        }
    }
}