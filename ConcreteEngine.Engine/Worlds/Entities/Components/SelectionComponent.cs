using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Engine.Worlds.Entities.Components;

public struct SelectionComponent() : IEntityComponent
{
    private static Color4 DefaultHighlight => Color4.FromRgba(46, 163, 242);
    
    public Color4 HighlightColor = DefaultHighlight;
    public float LineIntensity = 0.5f;
/*
    public float ScrollSpeed = 0.1f;
    public float LineDensity = 1.2f;
    public float LineThickness = 0.05f;
    public float PulseSpeed = 0.25f;
*/
}

public struct DebugBoundsComponent() : IEntityComponent
{
    public Color4 Color = Color4.Green;
    public float LineThickness;
    public bool ByPart;
}
