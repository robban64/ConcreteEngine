namespace ConcreteEngine.Engine.Scene.Template;

public sealed class SceneObjectTemplate
{
    public required string Name;
    public List<RenderEntityTemplate> RenderEntities = [];
    public List<GameEntityTemplate> GameEntities = [];
}