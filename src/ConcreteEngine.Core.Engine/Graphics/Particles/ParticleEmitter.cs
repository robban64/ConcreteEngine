using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Graphics.Particles;

[Inspect]
public sealed class ParticleEmitter : IComparable<ParticleEmitter>, IComparable<ushort>, IDisposable
{
    public const int CountAlignment = 16;

    public const int MinCount = 16;
    public const int MaxCount = 8192;
    public const int MaxBlockCount = MaxCount / 64;

    //

    public readonly Id16<ParticleEmitter> Id;

    public readonly string Name;

    [InspectInclude] public readonly ParticleEmitterState State;

    private readonly ParticleEmitterData _data;

    public MeshId BoundMesh { get; private set; }
    public int ParticleCount { get; private set; }
    public int PendingParticleCount { get; private set; }

    private BoundingBox _localBounds;

    public ParticleEmitter(string name, Id16<ParticleEmitter> id, int particleCount,
        in EmitterParams emitterParams, in ParticleParams particleParams)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id.Id);
        ArgumentOutOfRangeException.ThrowIfLessThan(particleCount, MinCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(particleCount, MaxCount);

        //_rng = new FastRandom((uint)Environment.TickCount + Id.Id);

        Name = name;
        Id = id;
        ParticleCount = PendingParticleCount = particleCount;
        State = new ParticleEmitterState(in emitterParams, in particleParams);

        _data = new ParticleEmitterData(particleCount);
        InitializeParticles(0, particleCount);
    }

    public int AlignedParticleCount => IntMath.AlignUp(ParticleCount, CountAlignment);

    public bool IsAttached => BoundMesh >= 0;
    public bool IsDirty => PendingParticleCount > 0 || State.HasDirtyVisual;

    public ref readonly BoundingBox LocalBounds => ref _localBounds;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ParticleEmitterData GetData()
    {
        if (_data.IsNullOrEmpty) Throwers.NullPointer("ParticleEmitter: null or empty emitter data");
        return _data;
    }

    public void SetCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, MinCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, MaxCount);

        if (count == ParticleCount || count == PendingParticleCount) return;
        PendingParticleCount = count;
    }


    internal void Attach(MeshId meshId)
    {
        if (BoundMesh.IsValid()) Throwers.InvalidOperation(nameof(BoundMesh));
        if(!meshId.IsValid())  Throwers.InvalidArgumentHandle(meshId);
        BoundMesh = meshId;
        UpdateLocalBounds();
    }

    internal void Commit()
    {
        if (State.HasDirtyVisual)
        {
            _data.UpdateLut(State.StartColor, State.EndColor, State.SizeStartEnd);
            State.HasDirtyVisual = false;
        }

        if (PendingParticleCount != ParticleCount)
        {
            int prevCount = ParticleCount, newCount = PendingParticleCount;
            _data.EnsureAllocate(newCount);

            ParticleCount = newCount;
            PendingParticleCount = 0;

            if (newCount > prevCount)
                InitializeParticles(prevCount, newCount - prevCount);
        }
    }

    internal void Simulate(float simDt)
    {
        if (_data.IsNullOrEmpty) Throwers.NullPointer("ParticleEmitter: null or empty emitter data");

        var count = AlignedParticleCount;
        var capacity = BitSet.GetCapacity256(count);
        if (capacity == 0 || (uint)capacity > MaxBlockCount) Throwers.InvalidOperation(nameof(capacity));

        Span<Bit64> deadBits = stackalloc Bit64[capacity];
        
        var deadCount = _data.SimulateLife(count, simDt, deadBits);
        if (deadCount > 0)
        {
            _data.RespawnParticles(deadCount, State, deadBits);
        }
        _data.SimulateSpatial(count, State.Gravity, simDt);
    }


    private void InitializeParticles(int start, int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)start + (uint)length, (uint)ParticleCount);
        var state = State;
        for (var i = start; i < length; i++) _data.Respawn(i, state);
    }

    private void UpdateLocalBounds()
    {
        var max = Vector3.One * 5;
        _localBounds = new BoundingBox(-max, max);
    }

    //
    public int CompareTo(ParticleEmitter? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Id.CompareTo(other.Id);
    }

    public int CompareTo(ushort other) => Id.CompareTo(other);

    public void Dispose()
    {
        _data.Dispose();
        BoundMesh = default;
    }

    //
}