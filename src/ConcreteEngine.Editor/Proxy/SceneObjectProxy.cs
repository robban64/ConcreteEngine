using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Editor.Proxy;

public enum ProxyPropertyKind : byte
{
    Spatial,
    Source,
    Particle,
    Animation
}

public sealed class SceneObjectProxy(ISceneObject sceneObject, SceneProxyProperties properties)
{
    public readonly SceneObjectId Id = sceneObject.Id;
    public readonly ISceneObject SceneObject = sceneObject;
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
    public bool IsEditing;

    public required Action<T> Setter;
    public required Action<T> Getter;

    public void InvokeSet() => Setter((T)this);
    public void InvokeGet() => Getter((T)this);

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
    public TransformStable Transform;
    public BoundingBox Bounds;

    public void Fill(in Transform transform, in BoundingBox bounds)
    {
        ref var t = ref Transform;
        TransformStable.From(in transform, t.EulerAngles, out t);
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