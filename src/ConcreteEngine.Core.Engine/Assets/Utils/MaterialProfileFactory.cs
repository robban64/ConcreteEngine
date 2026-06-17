using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets.Utils;


internal static class MaterialProfileFactory
{
    internal static MaterialProfileEntry[] CreateProfiles()
    {
        var entries = new MaterialProfileEntry[9];
        entries[(int)MaterialProfile.None] = ModelProfile;
        entries[(int)MaterialProfile.StaticModel] = ModelProfile;
        entries[(int)MaterialProfile.ModelTransparent] = ModelTransparentProfile;
        entries[(int)MaterialProfile.AnimatedModel] = ModelAnimatedProfile;
        entries[(int)MaterialProfile.Terrain] = TerrainProfile;
        entries[(int)MaterialProfile.Sky] = SkyProfile;
        entries[(int)MaterialProfile.Water] = SkyProfile;
        entries[(int)MaterialProfile.Particle] = ParticleProfile;
        entries[(int)MaterialProfile.Foliage] = FoliageProfile;
        return entries;
    }

    private static MaterialProfileEntry ModelProfile =>
        new(
            "Model",
            TextureUsage.Albedo, TextureUsage.Normal, TextureUsage.Mask
        );

    private static MaterialProfileEntry ModelTransparentProfile =>
        new(
            "Model", DrawCommandQueue.Transparent,
            TextureUsage.Albedo, TextureUsage.Normal, TextureUsage.Mask
        )
        {
            StateValues = new MaterialParams(specular: 0, shininess: 0),
            DrawState = GfxDrawState.Set(
                GfxDrawFlags.DepthTest | GfxDrawFlags.DepthWrite | GfxDrawFlags.PolygonOffset | GfxDrawFlags.Ac2,
                disable: GfxDrawFlags.Cull | GfxDrawFlags.Blend),
            DrawFunctions = new GfxDrawFunctions(Depth: DepthMode.Lequal, Cull: CullMode.FrontCcw)
        };

    private static MaterialProfileEntry ModelAnimatedProfile =>
        new(
            "ModelAnimated",
            TextureUsage.Albedo, TextureUsage.Normal, TextureUsage.Mask
        );

    private static MaterialProfileEntry ParticleProfile =>
        new(
            "Particle", DrawCommandQueue.Particles,
            TextureUsage.Albedo
        )
        {
            DrawState = GfxDrawState.Set(
                GfxDrawFlags.Blend,
                GfxDrawFlags.DepthWrite | GfxDrawFlags.Ac2 | GfxDrawFlags.Cull
            ),
            DrawFunctions = new GfxDrawFunctions(BlendMode.Alpha)
        };

    private static MaterialProfileEntry TerrainProfile =>
        new(
            "Terrain",
            TextureUsage.Albedo, TextureUsage.Splatmap
        ) { StateValues = new MaterialParams(shininess: 4, specular: 0.02f) };

    private static MaterialProfileEntry FoliageProfile =>
        new(
            "Foliage", DrawCommandQueue.Transparent,
            TextureUsage.Albedo
        )
        {
            DrawState = GfxDrawState.Set(
                GfxDrawFlags.DepthTest | GfxDrawFlags.Ac2,
                GfxDrawFlags.DepthWrite | GfxDrawFlags.Cull | GfxDrawFlags.Blend
            ),
            DrawFunctions = new GfxDrawFunctions(Depth: DepthMode.Lequal)
        };

    private static MaterialProfileEntry SkyProfile =>
        new(
            "Skybox", DrawCommandQueue.Skybox,
            TextureUsage.Albedo
        )
        {
            DrawState = GfxDrawState.Disable(GfxDrawFlags.DepthWrite | GfxDrawFlags.Ac2 | GfxDrawFlags.PolygonOffset |
                                             GfxDrawFlags.Cull),
            DrawFunctions = new GfxDrawFunctions(Depth: DepthMode.Lequal)
        };
}