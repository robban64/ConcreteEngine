namespace ConcreteEngine.Renderer.Core;

public static class CoreShaders
{
    public static ShaderId DepthShader { get; set; }
    public static ShaderId CompositeShader { get; set; }
    public static ShaderId ColorFilterShader { get; set; }
    public static ShaderId PresentShader { get; set; }
    public static ShaderId HighlightShader { get; set; }
    public static ShaderId BoundingBoxShader { get; set; }
}