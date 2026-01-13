using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Engine.Scene;

namespace ConcreteEngine.Engine.Editor;

internal sealed class EditorSceneObjectProxy(SceneObject sceneObject) : SceneObjectProxy
{
    public override SceneObjectId Id => sceneObject.Id;
    public override Guid GId => sceneObject.GId;
    public override string GIdString { get; } = sceneObject.GId.ToString();
    public override string Name => sceneObject.Name;
    public override bool Enabled => sceneObject.Enabled;
    public override int GameEntitiesCount => sceneObject.GameEntitiesCount;
    public override int RenderEntitiesCount => sceneObject.RenderEntitiesCount;


    //public override ref readonly Transform GetTransform() => ref sceneObject.GetTransform();
}