using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.ECS.GameComponent;

namespace ConcreteEngine.Engine.Scene.Template;

public interface IGameComponentTemplate
{
}

public sealed class GameEntityTemplate
{
    public TransformTemplate? Transform;
    public BoundingBoxTemplate? BoundingBox;
    public VisibilityTemplate? Visibility;
    public RenderLinkTemplate? RenderLink;
    
    public readonly List<IGameComponentTemplate> Components = [];
}

public sealed class VisibilityTemplate : IGameComponentTemplate
{
    public bool Enabled = true;
}

public sealed class TransformTemplate : IGameComponentTemplate
{
    public Transform Transform;
}

public sealed class BoundingBoxTemplate : IGameComponentTemplate
{
    public BoundingBox LocalBounds;
}

public sealed class RenderLinkTemplate : IGameComponentTemplate
{
    public bool CreateRenderEntity = true;
}

public sealed class AnimationTemplate : IGameComponentTemplate
{
    public float Time;
    public float Duration;
    public float Speed;
    public short Clip;
    public AnimationState State;
}