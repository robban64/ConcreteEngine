using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.App.Scene;

internal static class SceneExtensions
{
    extension(SceneObjectKind kind)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ToColor() =>
            kind switch
            {
                SceneObjectKind.Empty => Palette32.TextSecondary,
                SceneObjectKind.Model => Palette32.Model,
                SceneObjectKind.Particle => Palette32.Material,
                _ => Throwers.Unreachable<uint>(nameof(kind))
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Icons ToIcon() =>
            kind switch
            {
                SceneObjectKind.Empty => Icons.Minus,
                SceneObjectKind.Model => Icons.Box,
                SceneObjectKind.Particle => Icons.Sparkles,
                _ => Throwers.Unreachable<Icons>(nameof(kind))
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ToIntIcon() =>
            kind switch
            {
                SceneObjectKind.Empty => IconNames.Minus,
                SceneObjectKind.Model => IconNames.Box,
                SceneObjectKind.Particle => IconNames.Sparkles,
                _ => Throwers.Unreachable<uint>(nameof(kind))
            };
    }
}