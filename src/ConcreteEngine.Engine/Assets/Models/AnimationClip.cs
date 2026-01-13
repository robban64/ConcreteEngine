using System.Numerics;

namespace ConcreteEngine.Core.Engine.Assets.Models;

public sealed class AnimationClip(string name, float duration, float ticksPerSecond)
{
    public string Name { get; set; } = name;

    public readonly float Duration = duration;
    public readonly float TicksPerSecond = ticksPerSecond;

    public Dictionary<int, Track> Tracks { get; } = [];

    public sealed class Track
    {
        public float[] PositionTimes { get; set; }
        public Vector3[] Positions { get; set; }

        public float[] RotationTimes { get; set; }
        public Quaternion[] Rotations { get; set; }

        public float[] ScaleTimes { get; set; }
        public Vector3[] Scales { get; set; }
    }
}