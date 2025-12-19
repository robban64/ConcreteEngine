namespace ConcreteEngine.Engine.ECS;

public sealed class EntityWorld
{
    public GameEntityHub GameEntity { get; }
    public RenderEntityHub RenderEntity { get; }

    public EntityWorld()
    {
        GameEntity = new GameEntityHub();
        RenderEntity = new RenderEntityHub();
    }
    
}