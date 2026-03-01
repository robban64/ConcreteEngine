using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Editor.Utils;

internal static class SceneExtensions
{
    extension(SceneObjectKind kind)
    {
        public string ToText() => kind switch
        {
            SceneObjectKind.Empty => "Empty",
            SceneObjectKind.Model => "Model",
            SceneObjectKind.Particle => "Particle",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}