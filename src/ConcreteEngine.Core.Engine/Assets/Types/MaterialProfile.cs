using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
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

    public readonly string ShaderName;
    public readonly DrawCommandQueue DrawQueue;

    public readonly MaterialShading Shading;

    public required MaterialStateRecord StateValues { get; init; }

    public GfxDrawState DrawState = GfxDrawState.Set(
        GfxDrawFlags.DepthTest | GfxDrawFlags.DepthWrite | GfxDrawFlags.Cull,
        GfxDrawFlags.Blend | GfxDrawFlags.Ac2
    );

    public GfxDrawFunctions DrawFunctions =
        new(BlendMode.Unset, CullMode.BackCcw, DepthMode.Less, PolygonOffsetLevel.None);

    private readonly TextureUsage[] _slots;

    public MaterialProfile(string shaderName, DrawCommandQueue drawQueue, params TextureUsage[] slots)
        : this(shaderName, drawQueue, DefaultToggle, slots) { }

    public MaterialProfile(string shaderName, params TextureUsage[] slots)
        : this(shaderName, DrawCommandQueue.Opaque, DefaultToggle, slots) { }

    public MaterialProfile(string shader, DrawCommandQueue queue, MaterialShading shading, params TextureUsage[] slots)
    {
        _slots = slots;
        ShaderName = shader;
        DrawQueue = queue;
        Shading = shading;
    }


    public int SlotsCount => _slots.Length;
    public ReadOnlySpan<TextureUsage> Slots => _slots;
    
    internal void AttachShader(Shader shader)
    {
        if (Shader != null!) throw new InvalidOperationException("Shader already attached");
        if (shader.Name != ShaderName) throw new ArgumentException(nameof(shader));
        Shader = shader;
    }

    public TextureUsage GetSlot(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_slots.Length, nameof(index));
        return _slots[index];
    }

    public TextureSource[] MakeSourceArray()
    {
        var sources = new TextureSource[_slots.Length];
        for (int i = 0; i < _slots.Length; i++)
            sources[i] = new TextureSource(default, _slots[i]);
        return sources;
    }

    public void WriteSources(TextureSource[] sources)
    {
        ValidateSources(sources);
        for (int i = 0; i < sources.Length; i++)
            sources[i] = sources[i] with { Usage = _slots[i] };
    }


    public void ValidateSources(ReadOnlySpan<TextureSource> sources)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(sources.Length, _slots.Length, nameof(sources));
        for (int i = 0; i < _slots.Length; i++)
            ArgumentOutOfRangeException.ThrowIfNotEqual((int)sources[i].Usage, (int)_slots[i], nameof(sources));
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
        new("Model", TextureUsage.Albedo, TextureUsage.Normal, TextureUsage.Mask)
        {
            StateValues = MaterialStateRecord.Make(0.12f, 12f)
        };

    private static MaterialProfile AnimatedProfile =>
        new("ModelAnimated", TextureUsage.Albedo, TextureUsage.Normal, TextureUsage.Mask)
        {
            StateValues = MaterialStateRecord.Make(0.12f, 12f)
        };

    
    private static MaterialProfile TransparentProfile =>
        new(
            "Model", DrawCommandQueue.Transparent,
            MaterialShading.Shadows | MaterialShading.Transparent,
            TextureUsage.Albedo, TextureUsage.Normal, TextureUsage.Mask
        )
        {
            StateValues = MaterialStateRecord.Make(0,0),
            DrawState = GfxDrawState.Set(
                GfxDrawFlags.Blend,
                GfxDrawFlags.DepthWrite | GfxDrawFlags.Ac2 | GfxDrawFlags.Cull
            ),
            DrawFunctions = new GfxDrawFunctions(Depth: DepthMode.Lequal)
        };

    private static MaterialProfile AlphaMaskedProfile =>
        new(
            "Model", DrawCommandQueue.Transparent,
            MaterialShading.Shadows | MaterialShading.Transparent,
            TextureUsage.Albedo, TextureUsage.Normal, TextureUsage.Mask
        )
        {
            StateValues = MaterialStateRecord.Make(0,0),
            DrawState = GfxDrawState.Set(
                GfxDrawFlags.DepthTest | GfxDrawFlags.DepthWrite | GfxDrawFlags.PolygonOffset | GfxDrawFlags.Ac2,
                disable: GfxDrawFlags.Cull | GfxDrawFlags.Blend),
            DrawFunctions = new GfxDrawFunctions(Depth: DepthMode.Lequal)
        };


    private static MaterialProfile ParticleProfile =>
        new("Particle", DrawCommandQueue.Particles, MaterialShading.Transparent, TextureUsage.Albedo)
        {
            StateValues = MaterialStateRecord.Make(0,0),
            DrawState = GfxDrawState.Set(
                GfxDrawFlags.Blend,
                GfxDrawFlags.DepthWrite | GfxDrawFlags.Ac2 | GfxDrawFlags.Cull
            ),
            DrawFunctions = new GfxDrawFunctions(BlendMode.Alpha)
        };

    private static MaterialProfile TerrainProfile =>
        new("Terrain", TextureUsage.Albedo, TextureUsage.Splatmap)
        {
            StateValues = MaterialStateRecord.Make(0.02f, 4f)
        };

    private static MaterialProfile FoliageProfile =>
        new(
            "Foliage", DrawCommandQueue.Transparent,
            MaterialShading.Transparent | MaterialShading.ReceiveShadows,
            TextureUsage.Albedo
        )
        {
            StateValues = MaterialStateRecord.Make(0,0),
            DrawState = GfxDrawState.Set(
                GfxDrawFlags.DepthTest | GfxDrawFlags.Ac2,
                GfxDrawFlags.DepthWrite | GfxDrawFlags.Cull | GfxDrawFlags.Blend
            ),
            DrawFunctions = new GfxDrawFunctions(Depth: DepthMode.Lequal)
        };

    private static MaterialProfile SkyProfile =>
        new("Skybox", DrawCommandQueue.Skybox, MaterialShading.DoubleSided, TextureUsage.Albedo)
        {
            StateValues = MaterialStateRecord.Make(0,0),
            DrawState = GfxDrawState.Disable(GfxDrawFlags.DepthWrite | GfxDrawFlags.Ac2 | GfxDrawFlags.PolygonOffset |
                                             GfxDrawFlags.Cull),
            DrawFunctions = new GfxDrawFunctions(Depth: DepthMode.Lequal)
        };
}