namespace ConcreteEngine.Renderer.Definitions;

public enum RenderPipelineVersion
{
    None,
    Default3D
}

public enum CoreShaderKind
{
    DepthShader,
    CompositeShader,
    ColorFilterShader,
    PresentShader
}