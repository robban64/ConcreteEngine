using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Proxy;

namespace ConcreteEngine.Editor.Controller;

public abstract class SceneController
{
    public abstract int Count { get; }
    public abstract int GetCountByKind(SceneObjectKind kind);
    public abstract SceneObjectHeader GetSceneObjectHeader(int index);
    public abstract SceneObjectProxy? GetProxy(SceneObjectId id);

    public abstract void Select(SceneObjectId id);
    public abstract void Deselect(SceneObjectId id);
}

public readonly ref struct SceneObjectHeader(string name, Guid gId, SceneObjectId id, bool enabled, SceneObjectKind kind)
{
    public readonly string Name = name;
    public readonly Guid GId = gId;
    public readonly SceneObjectId Id = id;
    public readonly bool Enabled = enabled;
    public readonly SceneObjectKind Kind = kind;
}