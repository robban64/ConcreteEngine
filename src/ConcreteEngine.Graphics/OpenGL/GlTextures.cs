using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using Silk.NET.OpenGL;
using static ConcreteEngine.Graphics.OpenGL.GlDriver;

namespace ConcreteEngine.Graphics.OpenGL;

internal static class GlTextures
{
    public static NativeHandle<TextureMeta> CreateTexture(TextureKind kind)
    {
        Gl.CreateTextures(kind.ToGlEnum(), 1, out uint texture);
        return new NativeHandle<TextureMeta>(texture);
    }

    public static NativeHandle<SamplerMeta> CreateSampler()
    {
        Gl.CreateSamplers(1, out uint samplerId);
        return new NativeHandle<SamplerMeta>(samplerId);
    }

    public static void TextureStorage2D(NativeHandle<TextureMeta> handle, Size2D size, GpuTextureProps desc)
    {
        (uint width, uint height) = size.ToUnsigned();
        Gl.TextureStorage2D(handle, desc.Levels, desc.Format.ToStorageFormat(), width, height);
    }

    public static void TextureStorage2D_MultiSample(NativeHandle<TextureMeta> handle, Size2D size, GpuTextureProps desc)
    {
        (uint width, uint height) = size.ToUnsigned();
        Gl.TextureStorage2DMultisample(handle, desc.Samples, desc.Format.ToStorageFormat(), width, height, true);
    }

    public static void TextureStorage3D(NativeHandle<TextureMeta> handle, Size3D size, GpuTextureProps desc)
    {
        (uint width, uint height, uint depth) = size.ToUnsigned();
        Gl.TextureStorage3D(handle, desc.Levels, desc.Format.ToStorageFormat(), width, height, depth);
    }

    public static void UploadTexture2D_Data(NativeHandle<TextureMeta> handle, ReadOnlySpan<byte> data,
        TexturePixelFormat format,
        Size2D size)
    {
        (uint width, uint height) = size.ToUnsigned();
        var (fmt, type) = format.ToUploadFormatType();
        Gl.TextureSubImage2D(handle, 0, 0, 0, width, height, fmt, type, data);
    }

    public static void UploadTexture3D_Data(NativeHandle<TextureMeta> handle, ReadOnlySpan<byte> data,
        TexturePixelFormat format,
        Size3D size, int zOffset)
    {
        (uint width, uint height, uint depth) = size.ToUnsigned();
        var (fmt, type) = format.ToUploadFormatType();
        Gl.TextureSubImage3D(
            handle, level: 0,
            xoffset: 0, yoffset: 0, zoffset: zOffset,
            width: width, height: height, depth: depth,
            format: fmt, type: type,
            pixels: data
        );
    }

    public static void CopyTextureData(
        NativeHandle<TextureMeta> src, TextureKind srcKind, NativeHandle<TextureMeta> dst, TextureKind dstKind,
        int srcLevel, int dstLevel, Size3D srcSize,
        Int3 srcPos = default, Int3 dstPos = default)
    {
        (uint width, uint height, uint depth) = srcSize.ToUnsigned();

        Gl.CopyImageSubData(
            src, srcKind.ToGlEnum(), srcLevel, srcPos.X, srcPos.Y, srcPos.Z,
            dst, dstKind.ToGlEnum(), dstLevel, dstPos.X, dstPos.Y, dstPos.Z,
            width, height, depth
        );
    }

    public static void GenerateMipMaps(NativeHandle<TextureMeta> handle) => Gl.GenerateTextureMipmap(handle);

    public static void SetLodBias(NativeHandle<TextureMeta> handle, float lodBias) =>
        Gl.TextureParameter(handle, GLEnum.TextureLodBias, lodBias);

    public static void SetSamplerLodBias(NativeHandle<SamplerMeta> handle, float lodBias) =>
        Gl.SamplerParameter(handle, GLEnum.TextureLodBias, lodBias);

    public static void SetAnisotropy(NativeHandle<TextureMeta> handle, int anisotropy) =>
        Gl.TextureParameter(handle, GLEnum.TextureMaxAnisotropy, anisotropy);

    public static void SetSamplerAnisotropy(NativeHandle<SamplerMeta> handle, int anisotropy) =>
        Gl.SamplerParameter(handle, GLEnum.TextureMaxAnisotropy, anisotropy);

    public static void SetBorder(NativeHandle<TextureMeta> handle, TextureBorder b)
    {
        var c = (int)b;
        Gl.TextureParameterI(handle, GLEnum.TextureBorderColor, stackalloc int[] { c, c, c, c });
    }

    public static void SetSamplerBorder(NativeHandle<SamplerMeta> handle, TextureBorder b)
    {
        var c = (int)b;
        Gl.SamplerParameterI(handle, GLEnum.TextureBorderColor, stackalloc int[] { c, c, c, c });
    }

    public static void SetCompareTextureFunc(NativeHandle<TextureMeta> handle, DepthMode depthMode)
    {
        var compareMode = (int)GLEnum.CompareRefToTexture;
        var depthFunc = (int)depthMode.ToGlEnum();
        Gl.TextureParameterI(handle, GLEnum.TextureCompareMode, in compareMode);
        Gl.TextureParameterI(handle, GLEnum.TextureCompareFunc, in depthFunc);
    }

    public static void SetSamplerCompareTextureFunc(NativeHandle<SamplerMeta> handle, DepthMode depthMode)
    {
        var compareMode = (int)GLEnum.CompareRefToTexture;
        var depthFunc = (int)depthMode.ToGlEnum();
        Gl.SamplerParameterI(handle, GLEnum.TextureCompareMode, in compareMode);
        Gl.SamplerParameterI(handle, GLEnum.TextureCompareFunc, in depthFunc);
    }

    public static void SetTexturePreset(NativeHandle<TextureMeta> handle, TexturePreset preset, bool wrapR)
    {
        var param = GetGlParameters(preset);
        Gl.TextureParameter(handle, GLEnum.TextureMinFilter, ref param.MinFilter);
        Gl.TextureParameter(handle, GLEnum.TextureMagFilter, ref param.MagFilter);
        Gl.TextureParameter(handle, GLEnum.TextureWrapS, ref param.Wrap);
        Gl.TextureParameter(handle, GLEnum.TextureWrapT, ref param.Wrap);
        if (wrapR) Gl.TextureParameter(handle, GLEnum.TextureWrapR, ref param.Wrap);
    }

    public static void ApplySamplerParameters(NativeHandle<SamplerMeta> samplerHandle, TexturePreset preset, bool wrapR)
    {
        var param = GetGlParameters(preset);
        Gl.SamplerParameterI(samplerHandle, GLEnum.TextureMinFilter, ref param.MinFilter);
        Gl.SamplerParameterI(samplerHandle, GLEnum.TextureMagFilter, ref param.MagFilter);
        Gl.SamplerParameterI(samplerHandle, GLEnum.TextureWrapS, ref param.Wrap);
        Gl.SamplerParameterI(samplerHandle, GLEnum.TextureWrapT, ref param.Wrap);
        if (wrapR) Gl.SamplerParameterI(samplerHandle, GLEnum.TextureWrapR, ref param.Wrap);
    }


    private static (int MinFilter, int MagFilter, int Wrap) GetGlParameters(TexturePreset preset)
    {
        return preset switch
        {
            TexturePreset.NearestClamp => ((int)GLEnum.Nearest, (int)GLEnum.Nearest, (int)GLEnum.ClampToEdge),
            TexturePreset.NearestClampBorder => ((int)GLEnum.Nearest, (int)GLEnum.Nearest, (int)GLEnum.ClampToBorder),
            TexturePreset.NearestRepeat => ((int)GLEnum.Nearest, (int)GLEnum.Nearest, (int)GLEnum.Repeat),
            TexturePreset.LinearClamp => ((int)GLEnum.Linear, (int)GLEnum.Linear, (int)GLEnum.ClampToEdge),
            TexturePreset.LinearClampBorder => ((int)GLEnum.Linear, (int)GLEnum.Linear, (int)GLEnum.ClampToBorder),
            TexturePreset.LinearRepeat => ((int)GLEnum.Linear, (int)GLEnum.Linear, (int)GLEnum.Repeat),
            TexturePreset.LinearMipmapClamp => ((int)GLEnum.LinearMipmapLinear, (int)GLEnum.Linear,
                (int)GLEnum.ClampToEdge),
            TexturePreset.LinearMipmapRepeat => ((int)GLEnum.LinearMipmapLinear, (int)GLEnum.Linear,
                (int)GLEnum.Repeat),
            TexturePreset.PremultipliedUi => ((int)GLEnum.Linear, (int)GLEnum.Linear, (int)GLEnum.ClampToEdge),
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, null)
        };
    }
}