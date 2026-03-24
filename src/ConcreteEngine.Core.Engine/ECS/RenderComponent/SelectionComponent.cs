using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.ECS.RenderComponent;

public struct SelectionComponent() : IRenderComponent<SelectionComponent>
{
    private static readonly Color4 DefaultHighlight = Color4.FromRgba(46, 163, 242);

    public Color4 HighlightColor = DefaultHighlight;

    public float LineIntensity = 0.5f;
/*
    public float ScrollSpeed = 0.1f;
    public float LineDensity = 1.2f;
    public float LineThickness = 0.05f;
    public float PulseSpeed = 0.25f;
*/
}