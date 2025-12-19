namespace ConcreteEngine.Engine.ECS.GameComponent;

public enum AnimationState
{
    None,
    Pause,
    Play,
    Blending,
}

public struct AnimationComponent : IGameComponent<AnimationComponent>
{
    public float Time;
    public float PrevTime;
    public float Duration;
    public float Speed;
    public short Clip;
    public AnimationState State;
}