using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlHelper
{
    private readonly GL _gl;
    public DeviceCapabilities Capabilities { get; private set; } = null!;

    public GlHelper(GL gl)
    {
        _gl = gl;
    }

    public void SetTextureParameters(TexturePreset preset, TextureAnisotropy anisotropy, float lodBias)
    {
        switch (preset)
        {
            case TexturePreset.NearestClamp:
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);
                break;

            case TexturePreset.NearestRepeat:
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.Repeat);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.Repeat);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);
                break;

            case TexturePreset.LinearClamp:
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
                break;

            case TexturePreset.LinearRepeat:
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.Repeat);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.Repeat);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
                break;

            case TexturePreset.LinearMipmapClamp:
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
                _gl.GenerateMipmap(TextureTarget.Texture2D);
                break;

            case TexturePreset.LinearMipmapRepeat:
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.Repeat);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.Repeat);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
                _gl.GenerateMipmap(TextureTarget.Texture2D);
                break;

            case TexturePreset.PremultipliedUI:
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
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
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMaxAnisotropy, anisotropyValue);

            if (lodBias != 0)
                _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureLodBias, lodBias);
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