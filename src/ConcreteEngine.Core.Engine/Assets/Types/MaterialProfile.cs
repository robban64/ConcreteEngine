using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Engine.Assets;

public enum MaterialProfileId : byte
{
    None,
    Opaque,
    OpaqueAnimated, //TODO Remove
    Transparent,
    AlphaMasked,
    Terrain,
    Sky,
    Water,
    Particle,
    Foliage
}

public sealed class MaterialProfile
{
    private const MaterialShading DefaultToggle = MaterialShading.Shadows;

    public Shader Shader { get; private set; } = null!;
    public required MaterialStateRecord StateValues { get; init; }

    public readonly string ShaderName;
    public readonly DrawQueue DrawQueue;
    public readonly MaterialShading Shading;

    public GfxDrawState DrawState = GfxDrawState.Set(
        GfxDrawFlags.DepthTest | GfxDrawFlags.DepthWrite | GfxDrawFlags.Cull,
        GfxDrawFlags.Blend | GfxDrawFlags.Ac2
    );

    public GfxDrawFunctions DrawFunctions =
        new(BlendMode.Unset, CullMode.BackCcw, DepthMode.Less, PolygonOffsetLevel.None);

    private readonly SamplerSlot[] _slots;

    public MaterialProfile(string shaderName, DrawQueue drawQueue, params SamplerSlot[] slots)
        : this(shaderName, drawQueue, DefaultToggle, slots)
    {
    }

    public MaterialProfile(string shaderName, params SamplerSlot[] slots)
        : this(shaderName, DrawQueue.Opaque, DefaultToggle, slots)
    {
    }

    public MaterialProfile(string shader, DrawQueue queue, MaterialShading shading, params SamplerSlot[] slots)
    {
        _slots = slots;
        ShaderName = shader;
        DrawQueue = queue;
        Shading = shading;
    }


    public int SlotsCount => _slots.Length;
    public ReadOnlySpan<SamplerSlot> Slots => _slots;

    internal void AttachShader(Shader shader)
    {
        if (Shader != null!) throw new InvalidOperationException("Shader already attached");
        if (shader.Name != ShaderName) throw new ArgumentException(nameof(shader));
        Shader = shader;
    }

    public SamplerSlot GetSlot(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_slots.Length, nameof(index));
        return _slots[index];
    }

    public TextureSource[] MakeSourceArray()
    {
        var sources = new TextureSource[_slots.Length];
        WriteSources(sources);
        return sources;
    }

    public void WriteSources(TextureSource[] sources)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(sources.Length, _slots.Length, nameof(sources));
        for (int i = 0; i < sources.Length; i++)
        {
            var usage = _slots[i];
            var fallback = GetFallbackTexture(usage);
            sources[i] = new TextureSource(default, default, fallback, SamplerProfile.TrilinearWrap, _slots[i]);
        }
    }


    // --
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TextureId GetFallbackTexture(SamplerSlot usage)
    {
        if (usage is >= SamplerSlot.DetailMap and <= SamplerSlot.FeatureMap1)
            return GfxTextures.Fallback.AlbedoId;
        
        return usage switch
        {
            SamplerSlot.Diffuse or SamplerSlot.Specular or SamplerSlot.Emissive => GfxTextures.Fallback.AlbedoId,
            SamplerSlot.Normal => GfxTextures.Fallback.NormalId,
            SamplerSlot.AlphaMask => GfxTextures.Fallback.AlphaMaskId,
            _ => throw new ArgumentOutOfRangeException(nameof(usage))
        };
    }

    // --

    internal static MaterialProfile[] CreateProfiles()
    {
        var entries = new MaterialProfile[10];
        entries[(int)MaterialProfileId.None] = OpaqueProfile;
        entries[(int)MaterialProfileId.Opaque] = OpaqueProfile;
        entries[(int)MaterialProfileId.OpaqueAnimated] = AnimatedProfile;
        entries[(int)MaterialProfileId.Transparent] = TransparentProfile;
        entries[(int)MaterialProfileId.AlphaMasked] = AlphaMaskedProfile;
        entries[(int)MaterialProfileId.Terrain] = TerrainProfile;
        entries[(int)MaterialProfileId.Sky] = SkyProfile;
        entries[(int)MaterialProfileId.Water] = SkyProfile;
        entries[(int)MaterialProfileId.Particle] = ParticleProfile;
        entries[(int)MaterialProfileId.Foliage] = FoliageProfile;
        return entries;
    }

    private static MaterialProfile OpaqueProfile =>
        new("Model", SamplerSlot.Diffuse, SamplerSlot.Normal, SamplerSlot.AlphaMask)
        {
            StateValues = MaterialStateRecord.Make(0.12f, 12f)
        };

    private static MaterialProfile AnimatedProfile =>
        new("ModelAnimated", SamplerSlot.Diffuse, SamplerSlot.Normal, SamplerSlot.AlphaMask)
        {
            StateValues = MaterialStateRecord.Make(0.12f, 12f)
        };


    private static MaterialProfile TransparentProfile =>
        new(
            "Model", DrawQueue.Transparent,
            MaterialShading.Shadows | MaterialShading.Transparent,
            SamplerSlot.Diffuse, SamplerSlot.Normal, SamplerSlot.AlphaMask
        )
        {
            StateValues = MaterialStateRecord.Make(0, 0),
            DrawState = GfxDrawState.Set(
                GfxDrawFlags.Blend,
                GfxDrawFlags.DepthWrite | GfxDrawFlags.Ac2 | GfxDrawFlags.Cull
            ),
            DrawFunctions = new GfxDrawFunctions(Depth: DepthMode.Lequal)
        };

    private static MaterialProfile AlphaMaskedProfile =>
        new(
            "Model", DrawQueue.Transparent,
            MaterialShading.Shadows | MaterialShading.Transparent,
            SamplerSlot.Diffuse, SamplerSlot.Normal, SamplerSlot.AlphaMask
        )
        {
            StateValues = MaterialStateRecord.Make(0, 0),
            DrawState = GfxDrawState.Set(
                GfxDrawFlags.DepthTest | GfxDrawFlags.DepthWrite | GfxDrawFlags.PolygonOffset | GfxDrawFlags.Ac2,
                disable: GfxDrawFlags.Cull | GfxDrawFlags.Blend),
            DrawFunctions = new GfxDrawFunctions(Depth: DepthMode.Lequal)
        };


    private static MaterialProfile ParticleProfile =>
        new("Particle", DrawQueue.Particles, MaterialShading.Transparent, SamplerSlot.Diffuse)
        {
            StateValues = MaterialStateRecord.Make(0, 0),
            DrawState = GfxDrawState.Set(
                GfxDrawFlags.Blend,
                GfxDrawFlags.DepthWrite | GfxDrawFlags.Ac2 | GfxDrawFlags.Cull
            ),
            DrawFunctions = new GfxDrawFunctions(BlendMode.Alpha)
        };

    private static MaterialProfile TerrainProfile =>
        new("Terrain", SamplerSlot.Diffuse, SamplerSlot.FeatureMap0) { StateValues = MaterialStateRecord.Make(0.02f, 4f) };

    private static MaterialProfile FoliageProfile =>
        new(
            "Foliage", DrawQueue.Transparent,
            MaterialShading.Transparent | MaterialShading.ReceiveShadows,
            SamplerSlot.Diffuse
        )
        {
            StateValues = MaterialStateRecord.Make(0, 0),
            DrawState = GfxDrawState.Set(
                GfxDrawFlags.DepthTest | GfxDrawFlags.Ac2,
                GfxDrawFlags.DepthWrite | GfxDrawFlags.Cull | GfxDrawFlags.Blend
            ),
            DrawFunctions = new GfxDrawFunctions(Depth: DepthMode.Lequal)
        };

    private static MaterialProfile SkyProfile =>
        new("Skybox", DrawQueue.Skybox, MaterialShading.DoubleSided, SamplerSlot.Diffuse)
        {
            StateValues = MaterialStateRecord.Make(0, 0),
            DrawState = GfxDrawState.Disable(GfxDrawFlags.DepthWrite | GfxDrawFlags.Ac2 | GfxDrawFlags.PolygonOffset |
                                             GfxDrawFlags.Cull),
            DrawFunctions = new GfxDrawFunctions(Depth: DepthMode.Lequal)
        };
}