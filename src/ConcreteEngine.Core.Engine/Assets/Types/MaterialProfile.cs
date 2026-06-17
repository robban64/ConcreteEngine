using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets;

public enum MaterialProfileId : byte
{
    None,
    Opaque,
    OpaqueAnimated, //TODO Remove
    Transparent,
    Terrain,
    Sky,
    Water,
    Particle,
    Foliage
}

public sealed class MaterialProfile(
    string shaderName,
    DrawCommandQueue drawQueue,
    MaterialToggle toggle,
    params TextureUsage[] slots)
{
    private const MaterialToggle DefaultToggle = MaterialToggle.DoubleSided | MaterialToggle.Shadows;

    public Shader Shader { get; private set; } = null!;

    public readonly string ShaderName = shaderName;
    public readonly DrawCommandQueue DrawQueue = drawQueue;

    public readonly MaterialToggle Toggle = toggle;

    public MaterialParams StateValues = new(Color4.White, 0.12f, 12f);

    public GfxDrawState DrawState = GfxDrawState.Set(
        GfxDrawFlags.DepthTest | GfxDrawFlags.DepthWrite | GfxDrawFlags.Cull,
        GfxDrawFlags.Blend | GfxDrawFlags.Ac2
    );

    public GfxDrawFunctions DrawFunctions =
        new(BlendMode.Unset, CullMode.BackCcw, DepthMode.Less, PolygonOffsetLevel.None);

    public MaterialProfile(string shaderName, DrawCommandQueue drawQueue, params TextureUsage[] slots)
        : this(shaderName, drawQueue, DefaultToggle, slots)
    {
    }

    public MaterialProfile(string shaderName, params TextureUsage[] slots)
        : this(shaderName, DrawCommandQueue.Opaque, DefaultToggle, slots)
    {
    }


    public int SlotsCount => slots.Length;
    public ReadOnlySpan<TextureUsage> Slots => slots;

    internal void AttachShader(Shader shader)
    {
        if (Shader != null!) throw new InvalidOperationException("Shader already attached");
        if (shader.Name != ShaderName) throw new ArgumentException(nameof(shader));
        Shader = shader;
    }

    public TextureUsage GetSlot(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)slots.Length, nameof(index));
        return slots[index];
    }

    public TextureSource[] MakeSourceArray()
    {
        var sources = new TextureSource[slots.Length];
        for (int i = 0; i < slots.Length; i++)
            sources[i] = new TextureSource(default, slots[i]);
        return sources;
    }

    public void WriteSources(TextureSource[] sources)
    {
        ValidateSources(sources);
        for (int i = 0; i < sources.Length; i++)
            sources[i] = sources[i] with { Usage = slots[i] };
    }


    public void ValidateSources(ReadOnlySpan<TextureSource> sources)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(sources.Length, slots.Length, nameof(sources));
        for (int i = 0; i < slots.Length; i++)
            ArgumentOutOfRangeException.ThrowIfNotEqual((int)sources[i].Usage, (int)slots[i], nameof(sources));
    }

    // --

    internal static MaterialProfile[] CreateProfiles()
    {
        var entries = new MaterialProfile[9];
        entries[(int)MaterialProfileId.None] = OpaqueProfile;
        entries[(int)MaterialProfileId.Opaque] = OpaqueProfile;
        entries[(int)MaterialProfileId.Transparent] = TransparentProfile;
        entries[(int)MaterialProfileId.OpaqueAnimated] = AnimatedProfile;
        entries[(int)MaterialProfileId.Terrain] = TerrainProfile;
        entries[(int)MaterialProfileId.Sky] = SkyProfile;
        entries[(int)MaterialProfileId.Water] = SkyProfile;
        entries[(int)MaterialProfileId.Particle] = ParticleProfile;
        entries[(int)MaterialProfileId.Foliage] = FoliageProfile;
        return entries;
    }

    private static MaterialProfile OpaqueProfile =>
        new("Model", TextureUsage.Albedo, TextureUsage.Normal, TextureUsage.Mask);

    private static MaterialProfile AnimatedProfile =>
        new("ModelAnimated", TextureUsage.Albedo, TextureUsage.Normal, TextureUsage.Mask);

    private static MaterialProfile TransparentProfile =>
        new(
            "Model", DrawCommandQueue.Transparent,
            MaterialToggle.Shadows | MaterialToggle.Transparent,
            TextureUsage.Albedo, TextureUsage.Normal, TextureUsage.Mask
        )
        {
            StateValues = new MaterialParams(specular: 0, shininess: 0),
            DrawState = GfxDrawState.Set(
                GfxDrawFlags.DepthTest | GfxDrawFlags.DepthWrite | GfxDrawFlags.PolygonOffset | GfxDrawFlags.Ac2,
                disable: GfxDrawFlags.Cull | GfxDrawFlags.Blend),
            DrawFunctions = new GfxDrawFunctions(Depth: DepthMode.Lequal)
        };


    private static MaterialProfile ParticleProfile =>
        new("Particle", DrawCommandQueue.Particles, MaterialToggle.Transparent, TextureUsage.Albedo)
        {
            DrawState = GfxDrawState.Set(
                GfxDrawFlags.Blend,
                GfxDrawFlags.DepthWrite | GfxDrawFlags.Ac2 | GfxDrawFlags.Cull
            ),
            DrawFunctions = new GfxDrawFunctions(BlendMode.Alpha)
        };

    private static MaterialProfile TerrainProfile =>
        new("Terrain", TextureUsage.Albedo, TextureUsage.Splatmap)
        {
            StateValues = new MaterialParams(shininess: 4, specular: 0.02f)
        };

    private static MaterialProfile FoliageProfile =>
        new(
            "Foliage", DrawCommandQueue.Transparent,
            MaterialToggle.Transparent | MaterialToggle.CastShadows,
            TextureUsage.Albedo
        )
        {
            DrawState = GfxDrawState.Set(
                GfxDrawFlags.DepthTest | GfxDrawFlags.Ac2,
                GfxDrawFlags.DepthWrite | GfxDrawFlags.Cull | GfxDrawFlags.Blend
            ),
            DrawFunctions = new GfxDrawFunctions(Depth: DepthMode.Lequal)
        };

    private static MaterialProfile SkyProfile =>
        new("Skybox", DrawCommandQueue.Skybox, MaterialToggle.DoubleSided, TextureUsage.Albedo)
        {
            DrawState = GfxDrawState.Disable(GfxDrawFlags.DepthWrite | GfxDrawFlags.Ac2 | GfxDrawFlags.PolygonOffset |
                                             GfxDrawFlags.Cull),
            DrawFunctions = new GfxDrawFunctions(Depth: DepthMode.Lequal)
        };
}