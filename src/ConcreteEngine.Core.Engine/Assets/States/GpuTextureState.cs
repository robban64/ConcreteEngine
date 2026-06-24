using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class GpuTextureState(Texture texture, TextureProperties props)
{
    //
    public float LodBias
    {
        get;
        set
        {
            if (FloatMath.NearlyEqual(field, value)) return;
            field = value;
            texture.MarkDirty(AssetDirtyFlag.State);
        }
    } = props.Lod;

    public TexturePixelFormat PixelFormat
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            texture.MarkDirty(AssetDirtyFlag.Structure);
        }
    } = props.PixelFormat;

    public TexturePreset Preset
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            texture.MarkDirty(AssetDirtyFlag.State);
        }
    } = props.Preset;

    public TextureKind TextureKind
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            texture.MarkDirty(AssetDirtyFlag.Metadata);
        }
    } = props.Kind;


    public AnisotropyLevel Anisotropy
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            texture.MarkDirty(AssetDirtyFlag.State);
        }
    } = props.Anisotropy;
}