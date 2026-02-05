using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Controller.Proxy;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Controller;

public abstract class SceneController
{
    public abstract int Count { get; }
    public abstract int GetCountByKind(SceneObjectKind kind);
    public abstract void GetSceneObjectHeader(SceneObjectId id, out SceneObjectItem result);
    public abstract SceneObjectProxy? GetProxy(SceneObjectId id);

    public abstract int FilterQuery(in SearchPayload<SceneObjectId> search, SearchFilter filter, SearchSceneObjectDel del);

    public abstract void Select(SceneObjectId id);
    public abstract void Deselect(SceneObjectId id);
}

public readonly ref struct SceneObjectItem(ReadOnlySpan<char> name, ulong nameKey, bool enabled, SceneObjectKind kind)
{
    public readonly ReadOnlySpan<char> Name = name;
    public readonly ulong NameKey = nameKey;
    public readonly bool Enabled = enabled;
    public readonly SceneObjectKind Kind = kind;
}