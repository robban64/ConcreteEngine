namespace ConcreteEngine.Core.Rendering.Definitions;

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