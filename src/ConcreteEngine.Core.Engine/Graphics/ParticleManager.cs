using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Core.Engine.Graphics;

internal sealed class ParticleManager
{
    public static readonly ParticleManager Instance = new();
    private static int _emitterIdCounter;
    private static Id16<ParticleEmitter> MakeId() => new(++_emitterIdCounter);

    private readonly List<ParticleEmitter> _emitters = new(4);
    private readonly List<ParticleEmitter> _pendingEmitters = new(4);

    private readonly Dictionary<string, ParticleEmitter> _byName = new(4);

    private ParticleManager() { }

    public int EmitterCount => _emitters.Count;
    public bool HasPendingEmitters => _pendingEmitters.Count > 0;
    public ReadOnlySpan<ParticleEmitter> GetEmitters() => CollectionsMarshal.AsSpan(_emitters);
    internal ReadOnlySpan<ParticleEmitter> GetPendingEmitters() => CollectionsMarshal.AsSpan(_pendingEmitters);

    public ParticleEmitter CreateEmitter(
        string name,
        int particleCount,
        in EmitterSpatialParams definition,
        in EmitterVisualParams visualParams
    )
    {
        if (_byName.ContainsKey(name)) Throwers.InvalidArgument(nameof(name));

        var emitterId = MakeId();

        if (_emitters.Count > 0 && GetOrNull(emitterId) != null)
            throw new InvalidOperationException($"Duplicated emitter id {emitterId}");

        var emitter = new ParticleEmitter(name, emitterId, particleCount, in definition, in visualParams);
        _pendingEmitters.Add(emitter);
        _emitters.Add(emitter);
        _byName.Add(emitter.Name, emitter);
        return emitter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(string name, out ParticleEmitter emitter) => _byName.TryGetValue(name, out emitter!);

    public ParticleEmitter? GetOrNull(Id16<ParticleEmitter> emitterId)
    {
        SearchMethod.BinarySearchManaged(GetEmitters(), emitterId.Value, out var emitter);
        return emitter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ParticleEmitter Get(Id16<ParticleEmitter> emitterId)
    {
        if (SearchMethod.BinarySearchManaged(GetEmitters(), emitterId.Value, out var emitter) == -1)
            Throwers.NotFoundBy("Missing emitter emitterId", emitterId);

        return emitter;
    }

    internal void ClearPendingEmitters()
    {
        foreach (var it in _pendingEmitters)
        {
            if (!it.IsAttached) Throwers.InvalidOperation("Emitter should be attached when cleared");
        }

        _pendingEmitters.Clear();
    }
}