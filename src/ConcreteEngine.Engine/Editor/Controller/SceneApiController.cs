using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Engine.Scene;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class SceneApiController(ApiContext context) : IEngineSceneController
{
    private readonly SceneWorld _scene = context.Scene;
    
    public List<EditorSceneObject> LoadSceneObjectList()
    {
        var sceneObjects = _scene.Store.GetSceneObjectSpan();
        var result = new List<EditorSceneObject>(sceneObjects.Length);
        foreach (var it in sceneObjects)
        {
            var item = new EditorSceneObject
            {
                Id = new EditorId(it.Id, EditorItemType.SceneObject),
                EngineGid = it.Guid,
                Generation = it.Id.Gen,
                Name = it.Name,
                Enabled = it.Enabled,
                GameEcsCount = it.GameEntitiesCount,
                RenderEcsCount = it.RenderEntitiesCount
                
            };
            result.Add(item);
        }
        return result;
    } 
}