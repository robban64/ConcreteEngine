namespace ConcreteEngine.Engine.Assets.Descriptors;

internal static class CoreShaderManifest
{
    
    private static ShaderDescriptor[] ManifestRecords =>
    [
        CompositeManifest,
        PresentManifest,
        ColorFilterManifest,
        TerrainManifest,
        SkyboxManifest,
        ModelManifest,
        DepthManifest,
        HighlightManifest,
        BoundingManifest
    ];
    
    /*
    public static Dictionary<string, ShaderDescriptor> GetManifestDict =>
        new()
        {
            ["Composite"] = CompositeManifest,
            ["Present"] = PresentManifest,
            ["ColorFilter"] = ColorFilterManifest,
            ["Terrain"] = TerrainManifest,
            ["Skybox"] = SkyboxManifest,
            ["Model"] = ModelManifest,
            ["Depth"] = DepthManifest,
            ["Highlight"] = HighlightManifest,
            ["BoundingBox"] = BoundingManifest
        };
*/

    public static ShaderManifest GetManifest => new() { Records = ManifestRecords };

    public static ShaderDescriptor CompositeManifest => new("Composite", "screen.vert.glsl", "composite.frag.glsl");

    public static ShaderDescriptor PresentManifest => new("Present", "screen.vert.glsl", "present.frag.glsl");

    public static ShaderDescriptor ColorFilterManifest =>
        new("ColorFilter", "screen.vert.glsl", "color-filter.frag.glsl");

    public static ShaderDescriptor TerrainManifest => new("Terrain", "terrain.vert.glsl", "terrain.frag.glsl");

    public static ShaderDescriptor SkyboxManifest => new("Skybox", "skybox.vert.glsl", "skybox.frag.glsl");

    public static ShaderDescriptor ModelManifest => new("Model", "model.vert.glsl", "model.frag.glsl");

    public static ShaderDescriptor DepthManifest => new("Depth", "depth.vert.glsl", "depth.frag.glsl");
    
    public static ShaderDescriptor HighlightManifest => new("Highlight", "model-plain.vert.glsl", "highlight.frag.glsl");
    public static ShaderDescriptor BoundingManifest => new("BoundingBox", "model-plain.vert.glsl", "bounding-box.frag.glsl");

}