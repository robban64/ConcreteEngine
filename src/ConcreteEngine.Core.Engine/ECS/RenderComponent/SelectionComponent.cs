using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.ECS.RenderComponent;

public struct SelectionComponent(Color32 highlightColor) : IRenderComponent<SelectionComponent>
{
    public static Color32 DefaultHighlight => new(46, 163, 242);

    public Color32 HighlightColor = highlightColor;
    
/*
    public float ScrollSpeed = 0.1f;
    public float LineDensity = 1.2f;
    public float LineThickness = 0.05f;
    public float PulseSpeed = 0.25f;
*/
}