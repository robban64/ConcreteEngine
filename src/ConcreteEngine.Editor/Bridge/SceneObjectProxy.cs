using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Editor.Bridge;

public enum ProxyPropertyKind : byte
{
    Spatial,
    Source,
    Particle,
    Animation
}

public sealed class SceneObjectProxy(SceneObjectId id, string name, SceneProxyProperties properties)
{
    public readonly string Name = name;
    public readonly SceneObjectId Id = id;
    public readonly SceneProxyProperties Properties = properties;

    public void Refresh()
    {
        var props = Properties;
        props.SpatialProperty.InvokeGet();
        props.AnimationProperty?.InvokeGet();
        props.ParticleProperty?.InvokeGet();
    }
}

public sealed class SceneProxyProperties
{
    public required SpatialProperty SpatialProperty;
    public required SourceProperty SourceProperty;
    public ParticleProperty? ParticleProperty;
    public AnimationProperty? AnimationProperty;
}

public abstract class ProxyPropertyEntry<T> where T : ProxyPropertyEntry<T>
{
    public bool IsMixed;
    public bool IsReadOnly;

    public required Action<T> Setter;
    public required Action<T> Getter;

    internal void InvokeSet() => Setter((T)this);
    internal void InvokeGet() => Getter((T)this);

    public abstract string Name { get; }
    public abstract ProxyPropertyKind Kind { get; }
}

public class SourceProperty : ProxyPropertyEntry<SourceProperty>
{
    public MeshId Mesh;
    public MaterialId MaterialId;
    public override string Name => "Source Settings";
    public override ProxyPropertyKind Kind => ProxyPropertyKind.Source;
}

public class SpatialProperty : ProxyPropertyEntry<SpatialProperty>
{
    public TransformEdit Transform;
    public BoundingBox Bounds;

    public void Fill(in Transform transform, in BoundingBox bounds)
    {
        ref var t = ref Transform;
        TransformEdit.From(in transform, t.EulerAngles, out t);
        Bounds = bounds;
    }

    public override string Name => "Spatial Settings";
    public override ProxyPropertyKind Kind => ProxyPropertyKind.Spatial;
}

public class ParticleProperty : ProxyPropertyEntry<ParticleProperty>
{
    public ParticleDefinition Definition;
    public ParticleState State;
    public int EmitterHandle;
    public int ParticleCount;

    public void Fill(int emitter, int particleCount, in ParticleDefinition def, in ParticleState state)
    {
        EmitterHandle = emitter;
        ParticleCount = particleCount;
        Definition = def;
        State = state;
    }

    public override string Name => "Emitter Settings";
    public override ProxyPropertyKind Kind => ProxyPropertyKind.Particle;
}

public class AnimationProperty : ProxyPropertyEntry<AnimationProperty>
{
    public required AnimationId Animation;
    public required int ClipCount;

    public int Clip;
    public float Time;
    public float Speed;
    public float Duration;


    public override string Name => "Animation Settings";
    public override ProxyPropertyKind Kind => ProxyPropertyKind.Animation;
}