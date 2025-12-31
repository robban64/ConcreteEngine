namespace ConcreteEngine.Engine.Scene.Template;

public sealed class SceneObjectTemplate
{
    public required string Name;
    public List<RenderEntityTemplate> RenderEntities = [];
    public List<GameEntityTemplate> GameEntities = [];
}

public sealed class EntityTemplate
{
    public RenderEntityTemplate? RenderEntity;
    public GameEntityTemplate? GameEntity;
}