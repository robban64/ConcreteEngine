using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Inspector;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Editor.Utils;

internal static class AssetsExtensions
{
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetIconAndColor(FileBinding binding, AssetKind kind, out uint icon, out uint color)
    {
        switch (binding)
        {
            case FileBinding.Unknown:
                icon = StyleMap.GetIntIcon(Icons.Folder);
                color = Palette32.TextPrimary;
                break;
            case FileBinding.RootFile:
                icon = StyleMap.GetIntIcon(kind.ToIcon());
                color = Palette32.TextLightBlue;
                break;
            case FileBinding.DependentFile:
                //icon = StyleMap.GetIntIcon(kind.ToFileIcon());
                icon = StyleMap.GetIntIcon(Icons.FileImage);
                color = Palette32.TextSecondary;
                break;
            case FileBinding.UnboundFile:
                icon = StyleMap.GetIntIcon(Icons.File);
                color = Palette32.TextMuted;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(binding), binding, null);
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (uint icon, uint color) GetIconAndColor(FileBinding binding, AssetKind kind)
    {
        return binding switch
        {
            FileBinding.Unknown => (StyleMap.GetIntIcon(Icons.Folder), Palette32.TextPrimary),
            FileBinding.RootFile => (StyleMap.GetIntIcon(kind.ToIcon()), Palette32.TextLightBlue),
            FileBinding.DependentFile => (StyleMap.GetIntIcon(kind.ToFileIcon()), Palette32.TextSecondary),
            FileBinding.UnboundFile => (StyleMap.GetIntIcon(Icons.File), Palette32.TextMuted),
            _ => throw new ArgumentOutOfRangeException(nameof(binding), binding, null)
        };
    }

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