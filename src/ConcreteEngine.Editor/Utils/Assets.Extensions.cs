using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Inspector;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Editor.Utils;

internal static class AssetsExtensions
{
    extension(AssetKind kind)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Icons ToIcon()
        {
            return kind switch
            {
                AssetKind.Shader => AssetIcons.ShaderIcon,
                AssetKind.Model => AssetIcons.ModelIcon,
                AssetKind.Texture => AssetIcons.TextureIcon,
                AssetKind.Material => AssetIcons.MaterialIcon,
                _ => Throwers.Unreachable<Icons>(nameof(kind))
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Icons ToFileIcon()
        {
            return kind switch
            {
                AssetKind.Shader => AssetIcons.ShaderFileIcon,
                AssetKind.Model => AssetIcons.ModelFileIcon,
                AssetKind.Texture => AssetIcons.TextureFileIcon,
                AssetKind.Material => AssetIcons.MaterialIcon,
                _ => Throwers.Unreachable<Icons>(nameof(kind))
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
                _ => Throwers.Unreachable<string>(nameof(format))
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
                TextureKind.Texture2DArray => "CubeMap",
                TextureKind.Multisample2D => "Multisample",
                _ => Throwers.Unreachable<string>(nameof(kind))
            };
        }
    }
}