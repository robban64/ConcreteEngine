#region

using System.Numerics;

#endregion

namespace ConcreteEngine.Engine.Assets.Models;

public sealed class AnimationClip(string name, float duration, float ticksPerSecond)
{
    public string Name { get; set; } = name;
    
    public readonly float Duration = duration;
    public readonly float TicksPerSecond = ticksPerSecond;

    public Dictionary<int, Track> Tracks { get; } = [];

    public sealed class Track
    {
        public float[] PositionTimes { get; internal set; }
        public Vector3[] Positions { get; internal set; }

        public float[] RotationTimes { get; internal set; }
        public Quaternion[] Rotations { get; internal set; }

        public float[] ScaleTimes { get; internal set; }
        public Vector3[] Scales { get; internal set; }
    }
}