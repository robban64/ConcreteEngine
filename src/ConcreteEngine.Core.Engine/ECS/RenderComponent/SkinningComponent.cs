namespace ConcreteEngine.Core.Engine.ECS.RenderComponent;

public struct SkinningComponent(GameEntityId linkedAnimationEntity) : IRenderComponent<SkinningComponent>
{
    public GameEntityId LinkedAnimationEntity = linkedAnimationEntity;
}