using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.ECS.RenderComponent;

public struct DebugBoundsComponent() : IRenderComponent<DebugBoundsComponent>
{
    public Color4 Color = Color4.Green;
    public float LineThickness;
    public bool ByPart;
}