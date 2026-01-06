using System.Runtime.InteropServices;
using ConcreteEngine.Editor.Store.Resources;

namespace ConcreteEngine.Editor.Store;

internal static partial class ManagedStore
{
    public static ReadOnlySpan<EditorEntityResource> EntitySpan => CollectionsMarshal.AsSpan(_entityResources);
    public static ReadOnlySpan<EditorSceneObject> SceneObjectSpan => CollectionsMarshal.AsSpan(_sceneObjects);

}