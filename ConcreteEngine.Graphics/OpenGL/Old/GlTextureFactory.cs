using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlTextureFactory() : GlFactory()
{
    public GlTextureHandle CreateTexture2D(GpuTextureData data, in GpuTextureDescriptor desc, out TextureMeta meta)
    {
        var handle = Gl.GenTexture();
        Gl.BindTexture(GLEnum.Texture2D, handle);
        var (glFormat, glInternalFormat) = desc.Format.ToGlEnums();

        unsafe
        {
            if (desc.NullPtrData)
            {
                Gl.TexImage2D(GLEnum.Texture2D, 0, (int)glInternalFormat,
                    (uint)desc.Width, (uint)desc.Height, 0,
                    glFormat, GLEnum.UnsignedByte, (void*)0);
            }
            else
            {
                Gl.TexImage2D(GLEnum.Texture2D, 0, (int)glInternalFormat,
                    (uint)desc.Width, (uint)desc.Height, 0,
                    glFormat, GLEnum.UnsignedByte, data.PixelData);
            }
        }

        SetTextureParameters(desc.Preset, desc.Anisotropy, desc.LodBias);

        Gl.BindTexture(GLEnum.Texture2D, 0);

        meta = new TextureMeta(desc.Width, desc.Height, desc.Format);
        return new GlTextureHandle(handle);
    }

    public unsafe GlTextureHandle CreateCubeMap(GpuCubeMapData data, in GpuCubeMapDescriptor desc, out TextureMeta meta)
    {
        var (width, height) = (desc.Width, desc.Height);

        if (width != height)
            throw new InvalidOperationException("Width and Height are not the same size");

        if (width != desc.Width || height != desc.Height)
            throw new InvalidOperationException("Miss match between cubemap size");

        var target = (int)TextureTarget.TextureCubeMapPositiveX;

        var handle = Gl.GenTexture();
        Gl.BindTexture(GLEnum.TextureCubeMap, handle);
        var (format, internalFormat) = desc.Format.ToGlEnums();

        CreateFace(data.FaceData1, 0);
        CreateFace(data.FaceData2, 1);
        CreateFace(data.FaceData3, 2);
        CreateFace(data.FaceData4, 3);
        CreateFace(data.FaceData5, 4);
        CreateFace(data.FaceData6, 5);

        Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)GLEnum.ClampToEdge);
        Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        Gl.BindTexture(TextureTarget.TextureCubeMap, 0);

        meta = new TextureMeta(width, height, desc.Format);
        return new GlTextureHandle(handle);

        void CreateFace(ReadOnlySpan<byte> faceData, int face)
        {
            Gl.TexImage2D((TextureTarget)(target + face), 0, (int)internalFormat,
                (uint)width, (uint)height, 0,
                format, GLEnum.UnsignedByte, faceData);
        }
    }
    
    
    private void SetTextureParameters(TexturePreset preset, TextureAnisotropy anisotropy, float lodBias)
    {
        switch (preset)
        {
            case TexturePreset.NearestClamp:
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);
                break;

            case TexturePreset.NearestRepeat:
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.Repeat);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.Repeat);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);
                break;

            case TexturePreset.LinearClamp:
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
                break;

            case TexturePreset.LinearRepeat:
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.Repeat);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.Repeat);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
                break;

            case TexturePreset.LinearMipmapClamp:
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
                Gl.GenerateMipmap(TextureTarget.Texture2D);
                break;

            case TexturePreset.LinearMipmapRepeat:
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.Repeat);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.Repeat);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
                Gl.GenerateMipmap(TextureTarget.Texture2D);
                break;

            case TexturePreset.PremultipliedUI:
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
                // Could add sRGB decode disable if doing manual gamma
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
        }

        bool isMipMap = preset == TexturePreset.LinearMipmapClamp || preset == TexturePreset.LinearMipmapRepeat;
        if (isMipMap)
        {
            var anisotropyValue = GetAnisotropy(anisotropy);
            if (anisotropyValue > 1)
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMaxAnisotropy, anisotropyValue);

            if (lodBias != 0)
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureLodBias, lodBias);
        }
    }

    private float GetAnisotropy(TextureAnisotropy anisotropy)
    {
        int value = anisotropy switch
        {
            TextureAnisotropy.Off => 0,
            TextureAnisotropy.Default => 4,
            TextureAnisotropy.X2 => 2,
            TextureAnisotropy.X4 => 4,
            TextureAnisotropy.X8 => 8,
            TextureAnisotropy.X16 => 16,
            _ => throw new ArgumentOutOfRangeException(nameof(anisotropy), anisotropy, null),
        };

        return Math.Min(value, Capabilities.MaxAnisotropy);
    }
}