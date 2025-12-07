#region

using ConcreteEngine.Editor.Store;

#endregion

namespace ConcreteEngine.Editor.Data;

public readonly record struct EditorFetchHeader(EditorId EditorId)
{
    public static EditorFetchHeader Empty => new(EditorId.Empty);
}