using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets;

public enum MaterialProfile : byte
{
    None,
    StaticModel,
    ModelTransparent,
    AnimatedModel,
    Terrain,
    Sky,
    Water,
    Particle,
    Foliage
}

public sealed class MaterialProfileEntry(string shaderName, DrawCommandQueue drawQueue, params TextureUsage[] slots)
{
    public Shader Shader { get; private set; } = null!;

    public readonly string ShaderName = shaderName;
    public readonly DrawCommandQueue DrawQueue = drawQueue;

    public MaterialParams StateValues = new (Color4.White, 0.12f, 12f);

    public GfxDrawState DrawState = GfxDrawState.Set(
        GfxDrawFlags.DepthTest | GfxDrawFlags.DepthWrite | GfxDrawFlags.Cull,
        GfxDrawFlags.Blend | GfxDrawFlags.Ac2
    );

    public GfxDrawFunctions DrawFunctions =
        new(BlendMode.Unset, CullMode.BackCcw, DepthMode.Less, PolygonOffsetLevel.None);

    public MaterialProfileEntry(string shaderName, params TextureUsage[] slots)
        : this(shaderName, DrawCommandQueue.Opaque, slots)
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
}