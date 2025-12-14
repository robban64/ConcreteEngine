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


    private static ShaderDescriptor DepthManifest => new("Depth", "depth.vert.glsl", "depth.frag.glsl");


    private static ShaderDescriptor ModelManifest => new("Model", "model.vert.glsl", "model.frag.glsl");

    private static ShaderDescriptor ModelAnimatedManifest =>
        new("ModelAnimated", "model-animated.vert.glsl", "model.frag.glsl");

    private static ShaderDescriptor TerrainManifest => new("Terrain", "terrain.vert.glsl", "terrain.frag.glsl");
    private static ShaderDescriptor SkyboxManifest => new("Skybox", "skybox.vert.glsl", "skybox.frag.glsl");
    private static ShaderDescriptor ParticleManifest => new("Particle", "particle.vert.glsl", "particle.frag.glsl");


    private static ShaderDescriptor BoundingManifest =>
        new("BoundingBox", "model-plain.vert.glsl", "bounding-box.frag.glsl");

    private static ShaderDescriptor HighlightManifest =>
        new("Highlight", "model-plain.vert.glsl", "highlight.frag.glsl");


    private static ShaderDescriptor CompositeManifest => new("Composite", "screen.vert.glsl", "composite.frag.glsl");

    private static ShaderDescriptor PresentManifest => new("Present", "screen.vert.glsl", "present.frag.glsl");

    private static ShaderDescriptor ColorFilterManifest =>
        new("ColorFilter", "screen.vert.glsl", "color-filter.frag.glsl");
}