using System.Numerics;

namespace ConcreteEngine.Engine.Assets.Models;

public sealed class ModelAnimationData(string name, float duration, float ticksPerSecond)
{
    public string Name { get; set; } = name;
    public float Duration { get; set; } = duration;
    public float TicksPerSecond { get; set; } = ticksPerSecond;
    public Dictionary<int, BoneTrack> BoneTracksMap { get; } = [];

}

public sealed class BoneTrack
{
    public float[] TranslationTimes { get; internal set; }
    public Vector3[] Translations { get; internal set; }

    public float[] RotationTimes { get; internal set; }
    public Quaternion[] Rotations { get; internal set; }

    public float[] ScaleTimes { get; internal set; }
    public Vector3[] Scales { get; internal set; }
}

public struct BoneTransform
{
    public Vector3 Translation;
    public float TranslationTimes;
    public Vector3 Scale;
    public float ScaleTimes;
    public Quaternion Rotation;
    public float RotationTimes;
}