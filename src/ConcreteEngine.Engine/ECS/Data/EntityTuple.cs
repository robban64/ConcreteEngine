namespace ConcreteEngine.Engine.ECS.Data;

public readonly struct EntityTuple(GameEntityId gameEntityId, RenderEntityId renderEntityId)
{
    public readonly GameEntityId GameEntityId = gameEntityId;
    public readonly RenderEntityId RenderEntityId = renderEntityId;
}