using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Graphics.Handles;

namespace ConcreteEngine.Core.Engine.Graphics;

public abstract class ParticleSystemCore
{
    public static ParticleSystemCore Instance { get; private set; } = null!;

    public abstract ParticleEmitter CreateEmitter(string name, int particleCount, in EmitterSpatialParams definition,
        in EmitterVisualParams visualParams);
    public abstract ReadOnlySpan<ParticleEmitter> GetEmitters();
    public abstract bool TryGetEmitter(string name, out ParticleEmitter emitter);
    public abstract ParticleEmitter GetEmitter(Id32<ParticleEmitter> emitterId);
    public abstract ParticleEmitter? GetEmitterOrNull(Id32<ParticleEmitter> emitterId);
    public abstract MeshId GetEmitterMesh(ParticleEmitter emitter);
}
