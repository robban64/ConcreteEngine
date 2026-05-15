using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Renderer.Buffer;

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
        foreach (var emitterId in ActiveEmitters)
        {
            var emitter = particleManager.GetEmitter(emitterId);
            particleManager.GetMeshWriteData(emitter, out var gpuView, out var cpuView);
            ref readonly var param = ref emitter.VisualParams();
            ColorRgba startColor = param.StartColor.ToRgba(), endColor = param.EndColor.ToRgba();
            ProcessEmitter(gpuView, cpuView, param.SizeStartEnd, startColor, endColor, timeOffset);
            particleManager.UploadEmitter(emitter);
        }
    }

    [SkipLocalsInit]
    private static unsafe void ProcessEmitter(
        NativeView<ParticleGpuInstance> gpuView,
        NativeView<ParticleCpuInstance> cpuView,
        Vector2 sizeStartEnd,
        ColorRgba colorStart,
        ColorRgba colorEnd,
        float timeOffset)
    {
        var end = gpuView + gpuView.Length;

        while (gpuView < end)
        {
            //var color = Color4.Lerp(in param.StartColor, in param.EndColor, t);
            //color.A = 4.0f * t * (1.0f - t);

            var t = 1f - cpuView.Ptr->Life / cpuView.Ptr->MaxLife;
            var newSize = float.Lerp(sizeStartEnd.X, sizeStartEnd.Y, t);

            gpuView.Ptr->PositionSize =
                new Vector4(cpuView.Ptr->Position + cpuView.Ptr->Velocity * timeOffset, newSize);
            gpuView.Ptr->Color = ColorRgba.Lerp(colorStart, colorEnd, (byte)(t * 255f));

            ++gpuView.Ptr;
            ++cpuView.Ptr;
        }
    }
}