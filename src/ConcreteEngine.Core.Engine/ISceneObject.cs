namespace ConcreteEngine.Core.Engine;

public interface ISceneObject
{
    SceneObjectId Id { get; }
    Guid GId { get; }
    string Name { get; }
    bool Enabled { get; }

    int GameEntitiesCount { get; }
    int RenderEntitiesCount { get; }
}