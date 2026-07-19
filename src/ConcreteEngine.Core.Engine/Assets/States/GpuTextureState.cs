using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class GpuTextureState(Texture texture, TextureProperties props)
{
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

    //
    [InputNumber(InputStyle.Slider, Min = -1, Max = 1)]
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


    [InputCombo(UseEnumExt = true, StartAt = 1)]
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


    [InputCombo(UseEnumExt = true)]
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