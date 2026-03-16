using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Theme;

namespace ConcreteEngine.Editor.Utils;

internal static class SceneExtensions
{
    extension(SceneObjectKind kind)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> ToText() => kind switch
        {
            SceneObjectKind.Empty => "Empty"u8,
            SceneObjectKind.Model => "Model"u8,
            SceneObjectKind.Particle => "Particle"u8,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Icons ToIcon() => kind switch
        {
            SceneObjectKind.Empty => Icons.Minus,
            SceneObjectKind.Model => Icons.Box,
            SceneObjectKind.Particle => Icons.Sparkles,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}