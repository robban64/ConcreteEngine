using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.EcsRender;
using ConcreteEngine.Core.Engine.EcsRender.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics.Particles;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Systems;

internal sealed class ParticleSystem : IDisposable
{
    private static bool _allocated;
    private readonly List<Id16<ParticleEmitter>> _processedEmitters = new(16);

    private readonly ParticleMesh _particleMesh;
    private readonly ParticleManager _particleManager;

    internal ParticleSystem(GfxContext gfx)
    {
        if (_allocated) Throwers.InvalidOperation("ParticleSystem already active");
        _allocated = true;
        _particleMesh = new ParticleMesh(gfx);
        _particleManager = ParticleManager.Instance;
    }

    internal void Commit()
    {
        if (_particleManager.HasPendingEmitters)
            CommitPending();

        _particleManager.CommitEmitters();
    }

    private void CommitPending()
    {
        foreach (var id in _particleManager.GetPendingEmitters())
        {
            var emitter = _particleManager.Get(id);
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
        _particleManager.Dispose();
        _particleMesh.Dispose();
    }

    internal void Simulate(float simDt)
    {
        if (_particleManager.EmitterCount == 0) return;

        _processedEmitters.Clear();
        foreach (var it in RenderEcs.Store<EmitterLink>().VisibilityQuery())
        {
            if (_processedEmitters.Contains(it.Component.EmitterId)) continue;
            var emitter = _particleManager.Get(it.Component.EmitterId);
            emitter.Simulate(simDt);

            _processedEmitters.Add(it.Component.EmitterId);
        }
    }


    internal void Execute()
    {
        var timeOffset = (float)(EngineTime.SimulationDelta * EngineTime.SimulationAlpha);
        foreach (var emitterId in _processedEmitters.AsSpan())
        {
            var emitter = _particleManager.Get(emitterId);

            var particles = emitter.GetParticleView();
            ProcessEmitter(particles, in emitter.GetParticleParams(), timeOffset);
            _particleMesh.UploadGpuData(emitter.BoundSlot, particles.Length);
        }
    }

    [SkipLocalsInit]
    private unsafe void ProcessEmitter(
        in NativeView<ParticleState> particles,
        in ParticleParams param,
        float timeOffset)
    {
        param.Deconstruct(out var startColor, out var endColor, out var sizeStartEnd);

        var lut = stackalloc (float Size, ColorRgba Color)[256];
        for (int i = 0; i < 256; i++)
        {
            float t = i / 255f;
            var size = float.Lerp(sizeStartEnd.X, sizeStartEnd.Y, t);
            var color = LerpSse(startColor, endColor, (byte)i);
            lut[i] = (size, color);
        }

        foreach (var it in ParticleEnumerator(particles))
        {
            var t = it.Item1.InvLife;
            var idx = (byte)(t * 255f + 0.5f);

            var pos = it.Item1.Position + it.Item1.Velocity * timeOffset;
            it.Item2 = new ParticleVertex(new Vector4(pos, lut[idx].Size), lut[idx].Color);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PtrEnumerator<ParticleState, ParticleVertex> ParticleEnumerator(NativeView<ParticleState> particles) =>
        new(particles, _particleMesh.GetBufferView(particles.Length));

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