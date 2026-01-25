using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Proxy;

namespace ConcreteEngine.Editor.Controller;

public abstract class SceneController
{
    public abstract int Count { get; }
    public abstract int GetCountByKind(SceneObjectKind kind);
    public abstract void GetSceneObjectHeader(SceneObjectId id, out SceneObjectItem result);
    public abstract SceneObjectProxy? GetProxy(SceneObjectId id);

    public abstract void FilterQuery(List<SceneObjectId> result, in SceneObjectFilter filter, SceneObjectQueryDel del);

    public abstract void Select(SceneObjectId id);
    public abstract void Deselect(SceneObjectId id);
}

public delegate bool SceneObjectQueryDel(in SceneObjectFilter filter, in SceneObjectItem item);

public readonly ref struct SceneObjectFilter
{
    public required ReadOnlySpan<char> SearchString { get; init; }
    public required ulong SearchKey { get; init; }
    public required ulong SearchMask { get; init; }
    public required bool? Enabled { get; init; }
    public required SceneObjectKind Kind { get; init; }
}

public readonly ref struct SceneObjectItem(ReadOnlySpan<char> name, ulong nameKey, bool enabled, SceneObjectKind kind)
{
    public readonly ReadOnlySpan<char> Name = name;
    public readonly ulong NameKey = nameKey;
    public readonly bool Enabled = enabled;
    public readonly SceneObjectKind Kind = kind;
}