using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Renderer.Buffer;

namespace ConcreteEngine.Engine.Render.Processor;

internal static class ParticleProcessor
{
    private static readonly HashSet<Id32<ParticleEmitter>> ActiveEmitters = new(16);

    internal static void Simulate(float simDt)
    {
        ActiveEmitters.Clear();
        var particleSystem = ParticleSystem.Instance;
        foreach (var it in Ecs.Render.Query<ParticleComponent>())
        {
            if (!ActiveEmitters.Add(it.Component.Emitter)) continue;

            var emitter = particleSystem.GetEmitter(it.Component.Emitter);
            ParticleSystem.SimulateEmitter(emitter, simDt);
        }
    }

    internal static void TagParticles(in DrawEntityContext ctx, ParticleSystem particleSystem)
    {
        foreach (var query in Ecs.Render.Query<ParticleComponent>())
        {
            var drawItem = ctx.TryGetVisible(query.Entity);
            if (drawItem.Entity == 0) continue;

            var component = query.Component;
            var emitter = particleSystem.GetEmitter(component.Emitter);
            drawItem.Command.InstanceCount = (uint)emitter.ParticleCount;
            drawItem.Command.MeshId = particleSystem.GetEmitterMesh(emitter);
            drawItem.Command.MaterialId = component.Material;
            drawItem.Meta = new DrawCommandMeta(DrawCommandId.Particle, DrawCommandQueue.Particles, PassMask.Main);
        }
    }

    internal static void Execute(ParticleSystem particleSystem)
    {
        var timeOffset = EngineTime.EnvironmentDelta * EngineTime.EnvironmentAlpha;
        foreach (var emitterId in ActiveEmitters)
        {
            var emitter = particleSystem.GetEmitter(emitterId);
            particleSystem.GetMeshWriteData(emitter, out var gpuView, out var cpuView);
            ref readonly var param = ref emitter.VisualParams();
            ColorRgba startColor = param.StartColor.ToRgba(), endColor = param.EndColor.ToRgba();
            ProcessEmitter(gpuView, cpuView, param.SizeStartEnd, startColor, endColor, timeOffset);

            particleSystem.UploadEmitter(emitter);
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
        var end = gpuView.Ptr + gpuView.Length;

        while (gpuView.Ptr < end)
        {
            //var color = Color4.Lerp(in param.StartColor, in param.EndColor, t);
            //color.A = 4.0f * t * (1.0f - t);

            var t = 1f - cpuView.Ptr->Life / cpuView.Ptr->MaxLife;
            var newSize = float.Lerp(sizeStartEnd.X, sizeStartEnd.Y, t);

            gpuView.Ptr->PositionSize = new Vector4(
                cpuView.Ptr->Position + cpuView.Ptr->Velocity * timeOffset,
                newSize
            );
            gpuView.Ptr->Color = LerpSse(colorStart, colorEnd, (byte)(t * 255f));

            ++gpuView;
            ++cpuView;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ColorRgba LerpSse(ColorRgba a, ColorRgba b, byte t)
    {
        var va = Sse41.ConvertToVector128Int32(
            Vector128.CreateScalarUnsafe(Unsafe.As<ColorRgba, uint>(ref a)).AsByte());
        var vb = Sse41.ConvertToVector128Int32(
            Vector128.CreateScalarUnsafe(Unsafe.As<ColorRgba, uint>(ref b)).AsByte());

        var vt = Vector128.Create((int)t);

        var lerped = Sse2.Add(va, Sse2.ShiftRightArithmetic(Sse41.MultiplyLow(Sse2.Subtract(vb, va), vt), 8));

        var packed = Sse2.PackUnsignedSaturate(
            Sse2.PackSignedSaturate(lerped, Vector128<int>.Zero),
            Vector128<short>.Zero);

        uint scalar = packed.AsUInt32().ToScalar();
        return Unsafe.As<uint, ColorRgba>(ref scalar);
    }
}