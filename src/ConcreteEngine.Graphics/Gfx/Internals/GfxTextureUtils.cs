using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Handles;

namespace ConcreteEngine.Graphics.Gfx.Internals;

internal static class GfxTextureUtils
{
    public static bool SupportsWrapR(TextureKind kind) => kind is TextureKind.CubeMap or TextureKind.Texture3D;

    public static bool GetMipValues(Size3D size, TexturePreset preset, out int levels)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size.Depth, 1, nameof(size.Depth));
        var mipPreset = preset is TexturePreset.LinearMipmapClamp or TexturePreset.LinearMipmapRepeat;
        levels = mipPreset ? CalcMipLevels(size.Width, size.Height, size.Depth) : 1;
        return mipPreset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalcMipLevels(int width, int height, int depth = 1)
    {
        if (width <= 0 || height <= 0 || depth <= 0) return 0;
        int size = int.Max(width, int.Max(height, depth));
        return (int)float.Floor(float.Log2(size)) + 1;
    }

    public static Size2D CalcMipSize(int mipLevel, Size2D size)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(mipLevel);
        if (size.IsNegativeOrZero()) throw new ArgumentOutOfRangeException(nameof(size));

        int w = int.Max(1, size.Width >> mipLevel);
        int h = int.Max(1, size.Height >> mipLevel);
        return new Size2D(w, h);
    }

    public static void ValidateTexture2DArrayMeta(in TextureMeta arrayMeta, in TextureMeta srcMeta)
    {
        if (srcMeta.Kind != TextureKind.Texture2D) throw new GraphicsException(nameof(srcMeta.Kind));
        if (arrayMeta.Kind != TextureKind.Texture2DArray) throw new GraphicsException(nameof(arrayMeta.Kind));

        if (arrayMeta.Depth < 2) throw new GraphicsException(nameof(arrayMeta.MipLevels));

        if (arrayMeta.PixelFormat != srcMeta.PixelFormat) throw new GraphicsException(nameof(srcMeta.PixelFormat));
        if (arrayMeta.MipLevels != srcMeta.MipLevels) throw new GraphicsException(nameof(srcMeta.MipLevels));
        if (arrayMeta.Width != srcMeta.Width || arrayMeta.Height != srcMeta.Height)
            throw new GraphicsException("Mismatch texture size");
    }

    public static void ValidateUploadSize(Size2D size, Size2D metaSize)
    {
        if (size.IsZero() || metaSize.IsNegative()) throw new ArgumentOutOfRangeException(nameof(size));
        if (size != metaSize)
            throw new GraphicsException($"Size {size} must match TextureMeta size {metaSize}");
    }

    public static void ValidateUploadSize3D(Size3D size, Size3D metaSize)
    {
        if (size.IsZero() || metaSize.AnyNegative()) throw new ArgumentOutOfRangeException(nameof(size));
        if (size != metaSize)
            throw new GraphicsException($"Size {size} must match TextureMeta size {metaSize}");
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ValidateRecreateTexture(Size3D size, int? samples, in TextureMeta meta)
    {
        if (meta.Kind == TextureKind.Unknown || meta.PixelFormat == TexturePixelFormat.Unknown)
            throw new GraphicsException("Invalid meta texture meta.");

        ValidateTextureKindSize(meta.Kind, size);

        if (meta.Kind != TextureKind.Multisample2D && samples is not null)
            throw new GraphicsException("Samples can only be set for Multisample2D.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ValidateTextureDescriptor(Size3D size, CreateTextureProps props)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(props.Format, TexturePixelFormat.Unknown);

        ValidateTextureKindSize(props.Kind, size);

        // MSAA
        bool isMsaa = props.Kind == TextureKind.Multisample2D;
        if (isMsaa && props.Samples == RenderBufferMsaa.None)
            throw new GraphicsException("Multisample2D must have MSAA != None");
        if (!isMsaa && props.Samples != RenderBufferMsaa.None)
            throw new GraphicsException("Non-multisample textures must have MSAA=None");

        var hasMips = GetMipValues(size, props.Preset, out var levels);

        if (isMsaa && levels != 1)
            throw new GraphicsException("Multisample textures cannot have mipmaps");

        if (!hasMips)
        {
            if (props.Anisotropy != TextureAnisotropy.Off)
                throw new GraphicsException("Anisotropy requires mipmaps");
            if ((float)props.Lod != 0f)
                throw new GraphicsException("LodBias requires mipmaps");
        }
    }

    private static void ValidateTextureKindSize(TextureKind kind, Size3D size)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(kind, TextureKind.Unknown);
        if (size.IsZero() || size.AnyNegative())
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be > 0");

        if (kind is TextureKind.Texture3D or TextureKind.Texture2DArray)
        {
            if (size.Depth <= 0) throw new GraphicsException("Texture3D/Texture2DArray require positive depth");
            if (kind == TextureKind.Texture2DArray && size.Depth <= 1)
                throw new GraphicsException("Texture2DArray requires depth > 1");
        }
        else if (size.Depth != 1)
        {
            throw new GraphicsException("Depth must be 1 for non-3D or 2DArray textures");
        }

        if (kind == TextureKind.CubeMap && size.Width != size.Height)
            throw new GraphicsException("CubeMap faces must be square (W==H)");
    }
}