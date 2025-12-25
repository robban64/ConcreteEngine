using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Store.Resources;

namespace ConcreteEngine.Editor;

public static class EditorApi
{
    public static Func<List<EditorEntityResource>> LoadEntityResources = null!;
}