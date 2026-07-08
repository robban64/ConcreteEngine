using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Core.Provider;

namespace ConcreteEngine.Editor.App.Assets;

internal static class AssetsExtensions
{
    extension(FileBinding binding)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Icons GetIcon(AssetKind kind) => binding switch
        {
            FileBinding.Unknown => Icons.FileHeadphone,
            FileBinding.RootFile => kind.ToIcon(),
            FileBinding.DependentFile => Icons.FileImage,
            FileBinding.UnboundFile => Icons.File,
            _ => Throwers.Unreachable<Icons>(nameof(binding))
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetColor() => binding switch
        {
            FileBinding.Unknown => Palette32.TextDisabled,
            FileBinding.RootFile => Palette32.TextLightBlue,
            FileBinding.DependentFile => Palette32.TextSecondary,
            FileBinding.UnboundFile => Palette32.TextMuted,
            _ => Throwers.Unreachable<uint>(nameof(binding))
        };
    }


    extension(AssetKind kind)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ToColor() => kind switch
        {
            AssetKind.Unknown => Palette32.TextMuted,
            AssetKind.Shader => Palette32.Shader,
            AssetKind.Model => Palette32.Model,
            AssetKind.Texture => Palette32.Texture,
            AssetKind.Material => Palette32.Material,
            _ => Throwers.Unreachable<uint>(nameof(kind))
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Icons ToIcon() => kind switch
        {
            AssetKind.Shader => AssetIcons.ShaderIcon,
            AssetKind.Model => AssetIcons.ModelIcon,
            AssetKind.Texture => AssetIcons.TextureIcon,
            AssetKind.Material => AssetIcons.MaterialIcon,
            _ => Throwers.Unreachable<Icons>(nameof(kind))
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Icons ToFileIcon() => kind switch
        {
            AssetKind.Shader => AssetIcons.ShaderFileIcon,
            AssetKind.Model => AssetIcons.ModelFileIcon,
            AssetKind.Texture => AssetIcons.TextureFileIcon,
            AssetKind.Material => AssetIcons.MaterialIcon,
            _ => Throwers.Unreachable<Icons>(nameof(kind))
        };
    }


}