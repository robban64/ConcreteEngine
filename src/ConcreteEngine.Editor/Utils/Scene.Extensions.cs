using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Editor.Utils;

internal static class SceneExtensions
{
    extension(SceneObjectKind kind)
    {
        public ReadOnlySpan<byte> ToText8() => kind switch
        {
            SceneObjectKind.Empty => "Empty"u8,
            SceneObjectKind.Model => "Model"u8,
            SceneObjectKind.Particle => "Particle"u8,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}