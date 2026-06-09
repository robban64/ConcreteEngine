using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Graphics;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Engine.Render;

internal sealed class ParticleSystem : IDisposable
{
    private static bool _allocated;
    private readonly List<int> _processedEmitters = new(16);

    private readonly ParticleMesh _particleMesh;
    private readonly ParticleManager _particleManager;

    internal ParticleSystem(GfxContext gfx)
    {
        if(_allocated) Throwers.InvalidOperation("ParticleSystem already active");
        _allocated = true;
        _particleMesh = new ParticleMesh(gfx);
        _particleManager = ParticleManager.Instance;
    }

    internal void Commit()
    {
        if (!_particleManager.HasPendingEmitters) return;

        foreach (var emitter in _particleManager.GetPendingEmitters())
        {
            if (emitter.ParticleCount <= 0) Throwers.InvalidOperation(nameof(emitter.ParticleCount));
            var slot = _particleMesh.CreateParticleMesh(emitter.ParticleCount);
            var meshId = _particleMesh.GetHandle(slot).MeshId;
            emitter.Attach(slot, meshId);
        }
        
        _particleManager.ClearPendingEmitters();
    }

    public void Dispose()
    {
        _allocated = false;
        foreach (var emitter in _particleManager.GetEmitters()) emitter.Dispose();
    }

    internal void Simulate(float simDt)
    {
        if(_particleManager.EmitterCount == 0) return;
        
        _processedEmitters.Clear();
        foreach (var it in Ecs.GetRenderStore<ParticleComponent>().Query())
        {
            if (_processedEmitters.Contains(it.Component.EmitterId)) continue;
            var emitter = _particleManager.Get(it.Component.EmitterId);
            if(!emitter.IsAttached) continue;
            emitter.SimulateEmitter(simDt);
            
            _processedEmitters.Add(it.Component.EmitterId);
        }
    }

    
    internal unsafe void Upload()
    {
        var timeOffset = EngineTime.EnvironmentDelta * EngineTime.EnvironmentAlpha;
        foreach (var emitterId in CollectionsMarshal.AsSpan(_processedEmitters))
        {
            var emitter = _particleManager.Get((Id16<ParticleEmitter>)emitterId);
            
            var cpuView = emitter.GetParticleView();
            var gpuView = _particleMesh.GetBufferView(emitter.ParticleCount);
            
            ref readonly var param = ref emitter.VisualParams();
            ColorRgba startColor = param.StartColor.ToRgba(), endColor = param.EndColor.ToRgba();
            
            ProcessEmitter(gpuView.Length, gpuView, cpuView, param.SizeStartEnd, startColor, endColor, timeOffset);

            _particleMesh.UploadGpuData(emitter.Slot, emitter.ParticleCount);
        }
    }

    
    [SkipLocalsInit]
    private static unsafe void ProcessEmitter(
        int length,
        ParticleGpuInstance* gpuView,
        ParticleCpuInstance* cpuView,
        Vector2 sizeStartEnd,
        ColorRgba colorStart,
        ColorRgba colorEnd,
        float timeOffset)
    {
        var end = gpuView + length;

        while (gpuView < end)
        {
            //var color = Color4.Lerp(in param.StartColor, in param.EndColor, t);
            //color.A = 4.0f * t * (1.0f - t);

            var t = 1f - cpuView->Life / cpuView->MaxLife;
            var newSize = float.Lerp(sizeStartEnd.X, sizeStartEnd.Y, t);

            gpuView->PositionSize = new Vector4(
                cpuView->Position + cpuView->Velocity * timeOffset,
                newSize
            );
            gpuView->Color = LerpSse(colorStart, colorEnd, (byte)(t * 255f));

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