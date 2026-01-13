using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics.Gfx.Handles;

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

    public ProxyPropertyEntry<SpatialProperty>? GetSpatialProperty()
    {
        foreach (var it in Properties)
        {
            if (it is ProxyPropertyEntry<SpatialProperty> spatial) return spatial;
        }

        return null;
    }
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
    public bool IsEditing;

    public abstract void Refresh();

    public abstract Type ValueType { get; }
}

public sealed class ProxyPropertyEntry<T> : ProxyPropertyEntry where T : unmanaged
{
    private T _value;

    public required FuncFill<T> InvokeFetch;
    public required FuncIn<T, bool> InvokeSet;

    public ref readonly T Get() => ref _value;
    public void Set(in T value) => InvokeSet.Invoke(in value);

    public override void Refresh()
    {
        ref var snapshot = ref _value;
        InvokeFetch(out snapshot);
    }

    public override Type ValueType => typeof(T);
}

public struct SourceProperty(MeshId mesh, MaterialId materialId)
{
    public readonly MeshId Mesh = mesh;
    public readonly MaterialId MaterialId = materialId;
}

public struct SpatialProperty(in Transform transform, in BoundingBox bounds)
{
    public Transform Transform = transform;
    public BoundingBox Bounds = bounds;
}

public struct ParticleProperty(int handle, int count, in ParticleDefinition def, in ParticleState state)
{
    public ParticleDefinition Definition = def;
    public ParticleState State = state;
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