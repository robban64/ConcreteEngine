using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.App.Theme;
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
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ToIcon() =>
            kind switch
            {
                SceneObjectKind.Empty => IconNames.Minus,
                SceneObjectKind.Model => IconNames.Box,
                SceneObjectKind.Particle => IconNames.Sparkles,
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
    }
}