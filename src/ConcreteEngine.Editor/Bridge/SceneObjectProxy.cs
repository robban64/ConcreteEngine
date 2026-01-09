using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;

namespace ConcreteEngine.Editor.Bridge;

public abstract class SceneObjectProxy : ISceneObject
{
    public abstract SceneObjectId Id { get; }
    public abstract Guid GId { get; }
    public abstract string GIdString { get; }
    public abstract string Name { get; }
    public abstract bool Enabled { get; }
    public abstract int GameEntitiesCount { get; }
    public abstract int RenderEntitiesCount { get; }

    
    public required List<ProxyPropertyEntry> Properties;

    public ProxyPropertyEntry<SpatialProperty> GetSpatialProperty() =>
        (ProxyPropertyEntry<SpatialProperty>)Properties[0];
}

public enum ProxyPropertyKind : byte
{
    Spatial,
    Source,
    Particle,
    Animation
}

public abstract class ProxyPropertyEntry
{
    public required string Name;
    public required ProxyPropertyKind Kind;

    public bool IsMixed;
    public bool IsReadOnly;

    public abstract Type ValueType { get; }
}

public sealed class ProxyPropertyEntry<T> : ProxyPropertyEntry
{
    public override Type ValueType => typeof(T);
    public required Func<T> GetValue;
    public required Func<T, bool> SetValue;
}

public struct SourceProperty(ModelId model, int materialKey)
{
    public readonly int MaterialKey = materialKey;
    public readonly ModelId Model = model;
}

public struct SpatialProperty(in Transform transform, in BoundingBox bounds)
{
    public Transform Transform = transform;
    public BoundingBox Bounds = bounds;
}

public struct ParticleProperty(int handle, int count, in ParticleDefinition def, in ParticleEmitterState state)
{
    public ParticleDefinition Definition = def;
    public ParticleEmitterState EmitterState = state;
    public int EmitterHandle = handle;
    public int ParticleCount = count;
}

public struct AnimationProperty(AnimationId animationId, int clip, int clipCount)
{
    public float Time;
    public float Speed;
    public float Duration;

    public int ClipCount = clipCount;
    public int Clip = clip;
    public AnimationId Animation = animationId;
}