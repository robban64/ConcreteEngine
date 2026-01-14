using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Editor.Utils;

public static class AssetsExtensions
{
    extension(TexturePixelFormat format)
    {
        public ReadOnlySpan<byte> ToTextUtf8()
        {
            return format switch {
                TexturePixelFormat.Unknown => "Unknown"u8,
                TexturePixelFormat.Rgb => "Rgb"u8,
                TexturePixelFormat.Rgba => "Rgba"u8,
                TexturePixelFormat.SrgbAlpha => "Srgb"u8,
                TexturePixelFormat.Depth => "Depth"u8,
                TexturePixelFormat.Red => "Red"u8,
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };
        }
    }
    
    extension(TextureKind kind)
    {
        public ReadOnlySpan<byte> ToTextUtf8()
        {
            return kind switch {
                TextureKind.Unknown => "Unknown"u8,
                TextureKind.Texture2D => "Texture2D"u8,
                TextureKind.Texture3D => "Texture3D"u8,
                TextureKind.CubeMap => "CubeMap"u8,
                TextureKind.Multisample2D => "Multisample"u8,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }
    }
        
    extension(AssetKind kind)
    {
        public  Color4 ToColor()
        {
            return kind switch
            {
                AssetKind.Shader => new Color4(0.392f, 0.584f, 0.929f, 1.0f),
                AssetKind.Model => new Color4(1f, 0.647f, 0f, 1.0f),
                AssetKind.Texture => new Color4(0.4f, 0.4f, 0.8f, 1.0f),
                AssetKind.Material => new Color4(0.4f, 0.8f, 0.4f, 1.0f),
                _ => Color4.White
            };
        }

        public ReadOnlySpan<byte> ToTextUtf8()
        {
            return kind switch
            {
                AssetKind.Unknown => "Unknown"u8,
                AssetKind.Shader => "Shader"u8,
                AssetKind.Model => "Model"u8,
                AssetKind.Texture => "Texture"u8,
                AssetKind.Material => "Material"u8,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }

        public string ToText()
        {
            return kind switch
            {
                AssetKind.Unknown => "Unknown",
                AssetKind.Shader => "Shader",
                AssetKind.Model => "Model",
                AssetKind.Texture => "Texture",
                AssetKind.Material => "Material",
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }

        public ReadOnlySpan<byte> ToShortTextUtf8()
        {
            return kind switch
            {
                AssetKind.Unknown => "INV"u8,
                AssetKind.Shader => "SHD"u8,
                AssetKind.Model => "MOD"u8,
                AssetKind.Texture => "TEX"u8,
                AssetKind.Material => "MAT"u8,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }
        public string ToShortText()
        {
            return kind switch
            {
                AssetKind.Unknown => "INV",
                AssetKind.Shader => "SHD",
                AssetKind.Model => "MOD",
                AssetKind.Texture => "TEX",
                AssetKind.Material => "MAT",
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }
    }
}