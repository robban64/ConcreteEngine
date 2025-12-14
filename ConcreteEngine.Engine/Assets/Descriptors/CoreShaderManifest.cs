namespace ConcreteEngine.Engine.Assets.Descriptors;

internal static class CoreShaderManifest
{
    private static ShaderDescriptor[] ManifestRecords =>
    [
        DepthManifest,

        ModelManifest,
        ModelAnimatedManifest,
        TerrainManifest,
        SkyboxManifest,
        ParticleManifest,

        BoundingManifest,
        HighlightManifest,

        CompositeManifest,
        PresentManifest,
        ColorFilterManifest
    ];


    public static ShaderManifest GetManifest => new() { Records = ManifestRecords };


    private static ShaderDescriptor DepthManifest => new()
    {
        Name = "Depth",
        VertexFilename = "depth.vert.glsl",
        FragmentFilename = "depth.frag.glsl"
    };


    private static ShaderDescriptor ModelManifest => new()
    {
        Name = "Model",
        VertexFilename = "model.vert.glsl",
        FragmentFilename = "model.frag.glsl"
    };

    private static ShaderDescriptor ModelAnimatedManifest => new( )
        {
            Name = "ModelAnimated",
            VertexFilename =  "model-animated.vert.glsl",
            FragmentFilename = "model.frag.glsl"
        };

    private static ShaderDescriptor TerrainManifest => new()
    {
        Name = "Terrain",
        VertexFilename = "terrain.vert.glsl",
        FragmentFilename = "terrain.frag.glsl"
    };
    private static ShaderDescriptor SkyboxManifest => new()
    {
        Name = "Skybox",
        VertexFilename = "skybox.vert.glsl",
        FragmentFilename =  "skybox.frag.glsl"
    };
    private static ShaderDescriptor ParticleManifest => new()
    {
        Name = "Particle",
        VertexFilename = "particle.vert.glsl",
        FragmentFilename = "particle.frag.glsl"
    };


    private static ShaderDescriptor BoundingManifest => new()
        {
            Name = "BoundingBox",
            VertexFilename =  "model-plain.vert.glsl",
            FragmentFilename = "bounding-box.frag.glsl"
        };

    private static ShaderDescriptor HighlightManifest => new()
        {
            Name = "Highlight",
            VertexFilename = "model-plain.vert.glsl",
            FragmentFilename = "highlight.frag.glsl"
        };


    private static ShaderDescriptor CompositeManifest => new()
    {
        Name = "Composite",
        VertexFilename = "screen.vert.glsl",
        FragmentFilename = "composite.frag.glsl"
    };

    private static ShaderDescriptor PresentManifest => new()
    {
        Name = "Present", 
        VertexFilename = "screen.vert.glsl",
        FragmentFilename = "present.frag.glsl"
    };

    private static ShaderDescriptor ColorFilterManifest => new()
        {
            Name = "ColorFilter",
            VertexFilename = "screen.vert.glsl",
            FragmentFilename =  "color-filter.frag.glsl"
        };
}