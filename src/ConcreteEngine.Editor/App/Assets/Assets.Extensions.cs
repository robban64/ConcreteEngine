using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core.Data;

namespace ConcreteEngine.Editor.App.Assets;

internal static class AssetsExtensions
{
    public static Icons GetModelIcon(Model model) => model.Info.MeshCount > 1 ? Icons.Boxes : Icons.Box;
    public static Icons GetMaterialIcon(Material material) =>
        material.State.IsTransparent ? Icons.CircleDashed : Icons.Circle;

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
            AssetKind.Shader => Icons.Code,
            AssetKind.Model => Icons.Box,
            AssetKind.Texture => Icons.Image,
            AssetKind.Material => Icons.Circle,
            _ => Throwers.Unreachable<Icons>(nameof(kind))
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Icons ToFileIcon() => kind switch
        {
            AssetKind.Shader => Icons.FileCode,
            AssetKind.Model => Icons.FileBox,
            AssetKind.Texture => Icons.FileImage,
            AssetKind.Material => Icons.FileCog,
            _ => Throwers.Unreachable<Icons>(nameof(kind))
        };
    }


}