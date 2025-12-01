namespace ConcreteEngine.Engine.Worlds.Entities.Components;

public interface IRenderSourceComponent
{
    static abstract RenderSourceType SourceType { get; }
}
