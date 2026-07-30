using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.RenderEntity.RenderComponent;

public struct SelectionComponent(ColorRgba highlightColor) : IRenderComponent<SelectionComponent>
{
    public static SelectionComponent DefaultHighlight => new(new ColorRgba(46, 163, 242));

    public ColorRgba HighlightColor = highlightColor;

/*
    public float ScrollSpeed = 0.1f;
    public float LineDensity = 1.2f;
    public float LineThickness = 0.05f;
    public float PulseSpeed = 0.25f;
*/
}