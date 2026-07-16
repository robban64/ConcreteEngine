using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.App.Assets;

internal static class AssetsExtensions
{
    public static Icons GetModelIcon(Model model) => model.Info.MeshCount > 1 ? Icons.Boxes : Icons.Box;
    public static Icons GetMaterialIcon(Material material) =>
        material.State.IsTransparent ? Icons.CircleDashed : Icons.Circle;

    extension(FileBinding binding)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetIcon(AssetKind kind) => binding switch
        {
            FileBinding.Unknown => IconNames.FileHeadphone,
            FileBinding.RootFile => kind.ToIcon(),
            FileBinding.DependentFile => IconNames.FileImage,
            FileBinding.UnboundFile => IconNames.File,
            _ => Throwers.Unreachable<uint>(nameof(binding))
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
        public uint ToIcon() => kind switch
        {
            AssetKind.Shader => IconNames.Code,
            AssetKind.Model => IconNames.Box,
            AssetKind.Texture => IconNames.Image,
            AssetKind.Material => IconNames.Circle,
            _ => Throwers.Unreachable<uint>(nameof(kind))
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ToFileIcon() => kind switch
        {
            AssetKind.Shader => IconNames.FileCode,
            AssetKind.Model => IconNames.FileBox,
            AssetKind.Texture => IconNames.FileImage,
            AssetKind.Material => IconNames.FileCog,
            _ => Throwers.Unreachable<uint>(nameof(kind))
        };
    }


}