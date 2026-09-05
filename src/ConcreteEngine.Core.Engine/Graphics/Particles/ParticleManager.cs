using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Identity;

namespace ConcreteEngine.Core.Engine.Graphics.Particles;

internal sealed class ParticleManager : IDisposable
{
    public static readonly ParticleManager Instance = new();

    private readonly SlotArray<ParticleEmitter> _emitters = new(8);
    private readonly List<Id16<ParticleEmitter>> _pendingEmitters = new(4);


    private ParticleManager() { }

    public int EmitterCount => _emitters.Count;
    public bool HasPendingEmitters => _pendingEmitters.Count > 0;
    internal ReadOnlySpan<Id16<ParticleEmitter>> GetPendingEmitterIds() => CollectionsMarshal.AsSpan(_pendingEmitters);
    public ActiveObjectEnumerator<ParticleEmitter> EmitterEnumerator() => _emitters.GetEnumerator();
    
    public ParticleEmitter CreateEmitter(
        string name,
        int particleCount,
        in EmitterParams emitterParam,
        in ParticleParams particleParam
    )
    {
        foreach (var it in _emitters.AsSpan())
        {
            if (it?.Name == name) Throwers.InvalidArgument(nameof(name));
        }

        var emitterId = new Id16<ParticleEmitter>(_emitters.AllocateNextId() + 1);

        if (_emitters.Count > 0 && _emitters.GetOrNull(emitterId.Index) != null)
            throw new InvalidOperationException($"Duplicated emitter id {emitterId}");

        var emitter = new ParticleEmitter(name, emitterId, particleCount, in emitterParam, in particleParam);
        _pendingEmitters.Add(emitterId);
        _emitters.Set(emitter, emitterId.Index);
        return emitter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(string name, [NotNullWhen(true)] out ParticleEmitter? emitter)
    {
        foreach (var it in _emitters.AsSpan())
        {
            if (it?.Name == name)
            {
                emitter = it;
                return true;
            }
        }

        emitter = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ParticleEmitter Get(Id16<ParticleEmitter> emitterId)
    {
        if (!_emitters.TryGet(emitterId.Index, out var emitter))
            Throwers.NotFoundBy(nameof(emitterId), emitterId);

        return emitter;
    }

    internal void CommitEmitters()
    {
        foreach (var emitter in _emitters)
        {
            if (!emitter.IsDirty) emitter.Commit();
        }
    }

    internal void ClearPendingEmitters()
    {
        foreach (var id in _pendingEmitters)
        {
            if (!Get(id).IsAttached) Throwers.InvalidOperation("Emitter should be attached when cleared");
        }

        _pendingEmitters.Clear();
    }

    public void Dispose()
    {
        foreach (var emitter in _emitters) emitter.Dispose();
        _emitters.Clear();
    }
}