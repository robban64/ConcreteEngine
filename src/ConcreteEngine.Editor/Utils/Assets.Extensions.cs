using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Editor.Utils;

internal static class AssetsExtensions
{
    extension(AssetKind kind)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    extension(TexturePixelFormat format)
    {
        public string ToText()
        {
            return format switch
            {
                TexturePixelFormat.Unknown => "Unknown",
                TexturePixelFormat.Rgb => "Rgb",
                TexturePixelFormat.Rgba => "Rgba",
                TexturePixelFormat.SrgbAlpha => "Srgb",
                TexturePixelFormat.Depth => "Depth",
                TexturePixelFormat.Red => "Red",
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };
        }
    }

    extension(TextureKind kind)
    {
        public string ToText()
        {
            return kind switch
            {
                TextureKind.Unknown => "Unknown",
                TextureKind.Texture2D => "Texture2D",
                TextureKind.Texture3D => "Texture3D",
                TextureKind.CubeMap => "CubeMap",
                TextureKind.Multisample2D => "Multisample",
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }
    }
}