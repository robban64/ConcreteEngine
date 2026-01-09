using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Editor.Bridge;


/*

public abstract class SceneObjectProxy : ISceneObject
{
public abstract SceneObjectId Id { get; } // all fields are backed by an actual SceneObject
public abstract Guid GId { get; }
public abstract string Name { get; }
public abstract bool Enabled { get; }
public abstract int GameEntitiesCount { get; }
public abstract int RenderEntitiesCount { get; }

//
public required List<ProxyPropertyEntry> Properties;
}

public enum ProxyPropertyKind : byte
{
Spatial,
Source,
Particle,
Animation
}

// maybe not needed
public readonly record struct ProxyPropertyRequest(SceneObjectId Id, ProxyPropertyKind Kind);

public sealed class ProxyPropertyHeader
{
public required string Name;
public required bool IsMixed;
public required bool IsReadOnly;
}

public sealed class ProxyPropertyEntry
{
public readonly SceneObjectId ProxyId;
public readonly ProxyPropertyKind Kind;

//
public required Func<ProxyPropertyRequest, ProxyPropertyHeader> GetHeader;
public required Func<ProxyPropertyRequest, object> GetBody;
public required Action<ProxyPropertyRequest, object> Mutate;

//
public required Func<ProxyPropertyHeader> GetHeader;
public required Func<object> GetBody;
public required Action<object> Mutate;

//
public required Func<ProxyPropertyHeaderStruct> GetHeader;
public required Func<TStruct> GetBody;
public required Action<TStruct> Mutate;

// maybe
internal Action<ProxyPropertyEntry> OnRender;

public ProxyPropertyEntry(SceneObjectId proxyId, ProxyPropertyKind kind)
{
    ProxyId = proxyId;
    Kind = kind;
}
public ProxyPropertyHeader GetHeader() => _getHeader(new ProxyPropertyRequest(ProxyId, Kind));
public T GetBody<T>() where T : class => (T)_getBody(new ProxyPropertyRequest(ProxyId, Kind));

}


public struct ProxyPropertyHeaderRef
{
    public required string Name;
    public required bool IsMixed;
    public required bool IsReadOnly;
}

public ref struct ProxyPropertyPayload<T> where T : struct
{
    public readonly long Version;
    public ref readonly T Value;
}

public struct SpatialPropertyValue
{
    public Transform Transform;
    public BoundingBox Bounds;
}

public struct ParticlePropertyValue
{
    public ParticleDefinition Definition;
    public ParticleEmitterState EmitterState;
}*/
