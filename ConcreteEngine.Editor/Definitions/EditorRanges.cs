using System.Numerics;

namespace ConcreteEngine.Editor.Definitions;

internal sealed class EditorRanges
{
    public static class Bloom
    {
        public static readonly Vector2 IntensityRange = new(0f, 10f);
        public static readonly Vector2 ThresholdRange = new(0f, 5f);
        public static readonly Vector2 RadiusRange = new(0.1f, 7f);
    }

    public static class Fog
    {
        public static readonly Vector2 DensityRange = new(0f, 0.1f);
        public const bool UseLogarithmicSlider = true;
    }
}