using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;

namespace ConcreteEngine.Core.Engine.Graphics;

internal sealed class ParticleManager : IDisposable
{
    public static readonly ParticleManager Instance = new();

    private readonly SlotArray<ParticleEmitter> _emitters = new(8);
    private readonly List<Id16<ParticleEmitter>> _pendingEmitters = new(4);
    private readonly List<Id16<ParticleEmitter>> _dirtyEmitters = new(4);

    private readonly Dictionary<string, Id16<ParticleEmitter>> _byName = new(4);

    private ParticleManager() { }

    public int EmitterCount => _emitters.Count;
    public bool HasPendingEmitters => _pendingEmitters.Count > 0;
    internal ReadOnlySpan<Id16<ParticleEmitter>> GetPendingEmitters() => CollectionsMarshal.AsSpan(_pendingEmitters);

    public ParticleEmitter CreateEmitter(
        string name,
        int particleCount,
        in EmitterParams @params,
        in ParticleParams visualParams
    )
    {
        if (_byName.ContainsKey(name)) Throwers.InvalidArgument(nameof(name));

        var emitterId = new Id16<ParticleEmitter>(_emitters.AllocateNextId() + 1);

        if (_emitters.Count > 0 && _emitters.GetOrNull(emitterId.Index()) != null)
            throw new InvalidOperationException($"Duplicated emitter id {emitterId}");

        var emitter = new ParticleEmitter(name, emitterId, particleCount, in @params, in visualParams);
        _pendingEmitters.Add(emitterId);
        _emitters.Set(emitter, emitterId.Index());
        _byName.Add(emitter.Name, emitterId);
        return emitter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(string name, [NotNullWhen(true)] out ParticleEmitter? emitter)
    {
        if (_byName.TryGetValue(name, out var id) && _emitters.TryGet(id.Index(), out emitter)) return true;
        emitter = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ParticleEmitter Get(Id16<ParticleEmitter> emitterId)
    {
        if (!_emitters.TryGet(emitterId.Index(), out var emitter))
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
        _byName.Clear();
    }
}