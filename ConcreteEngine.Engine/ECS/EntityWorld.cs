namespace ConcreteEngine.Engine.ECS;

public sealed class EntityWorld
{
    private GameEntityHub GameEntity { get; }
    private RenderEntityHub RenderEntity { get; }

    public EntityWorld()
    {
        GameEntity = new GameEntityHub();
        RenderEntity = new RenderEntityHub();
    }
    
}